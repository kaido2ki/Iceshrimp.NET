using System.Collections.Immutable;
using CommunityToolkit.HighPerformance;
using SixLabors.ImageSharp.PixelFormats;

namespace Iceshrimp.Backend.Core.Helpers;

// Adapted from https://github.com/MarkusPalcer/blurhash.net under MIT
public static class BlurhashHelper
{
	private static readonly ImmutableArray<float> PrecomputedLut = [..Enumerable.Range(0, 256).Select(SRgbToLinear)];

	/// <summary>
	/// Encodes a Span2D of raw pixel data into a Blurhash string
	/// </summary>
	/// <param name="pixels">The Span2D of raw pixel data to encode</param>
	/// <param name="componentsX">The number of components used on the X-Axis for the DCT</param>
	/// <param name="componentsY">The number of components used on the Y-Axis for the DCT</param>
	/// <returns>The resulting Blurhash string</returns>
	public static string Encode(Span2D<Rgb24> pixels, int componentsX, int componentsY)
	{
		if (componentsX < 1) throw new ArgumentException("componentsX needs to be at least 1");
		if (componentsX > 9) throw new ArgumentException("componentsX needs to be at most 9");
		if (componentsY < 1) throw new ArgumentException("componentsY needs to be at least 1");
		if (componentsY > 9) throw new ArgumentException("componentsY needs to be at most 9");

		Span<Pixel> factors      = stackalloc Pixel[componentsX * componentsY];
		Span<char>  resultBuffer = stackalloc char[4 + 2 * componentsX * componentsY];
		Span<float> lut          = stackalloc float[256];
		PrecomputedLut.CopyTo(lut);

		var width  = pixels.Width;
		var height = pixels.Height;

		var xCosines = new double[width];
		var yCosines = new double[height];

		for (var yComponent = 0; yComponent < componentsY; yComponent++)
		for (var xComponent = 0; xComponent < componentsX; xComponent++)
		{
			double r             = 0, g = 0, b = 0;
			double normalization = xComponent == 0 && yComponent == 0 ? 1 : 2;

			for (var xPixel = 0; xPixel < width; xPixel++)
				xCosines[xPixel] = Math.Cos(Math.PI * xComponent * xPixel / width);
			for (var yPixel = 0; yPixel < height; yPixel++)
				yCosines[yPixel] = Math.Cos(Math.PI * yComponent * yPixel / height);

			for (var xPixel = 0; xPixel < width; xPixel++)
			for (var yPixel = 0; yPixel < height; yPixel++)
			{
				var basis = xCosines[xPixel] * yCosines[yPixel];
				var pixel = pixels[yPixel, xPixel];
				r += basis * lut[pixel.R];
				g += basis * lut[pixel.G];
				b += basis * lut[pixel.B];
			}

			var scale = normalization / (width * height);
			factors[componentsX * yComponent + xComponent].Red   = r * scale;
			factors[componentsX * yComponent + xComponent].Green = g * scale;
			factors[componentsX * yComponent + xComponent].Blue  = b * scale;
		}

		var dc      = factors[0];
		var acCount = componentsX * componentsY - 1;

		var sizeFlag = (componentsX - 1) + (componentsY - 1) * 9;
		sizeFlag.EncodeBase83(resultBuffer[..1]);

		float maximumValue;
		if (acCount > 0)
		{
			// Get maximum absolute value of all AC components
			var actualMaximumValue = 0.0;
			for (var yComponent = 0; yComponent < componentsY; yComponent++)
			for (var xComponent = 0; xComponent < componentsX; xComponent++)
			{
				// Ignore DC component
				if (xComponent == 0 && yComponent == 0) continue;

				var factorIndex = componentsX * yComponent + xComponent;

				actualMaximumValue = Math.Max(Math.Abs(factors[factorIndex].Red), actualMaximumValue);
				actualMaximumValue = Math.Max(Math.Abs(factors[factorIndex].Green), actualMaximumValue);
				actualMaximumValue = Math.Max(Math.Abs(factors[factorIndex].Blue), actualMaximumValue);
			}

			var quantizedMaximumValue = (int)Math.Max(0.0, Math.Min(82.0, Math.Floor(actualMaximumValue * 166 - 0.5)));
			maximumValue = ((float)quantizedMaximumValue + 1) / 166;
			quantizedMaximumValue.EncodeBase83(resultBuffer.Slice(1, 1));
		}
		else
		{
			maximumValue    = 1;
			resultBuffer[1] = '0';
		}

		EncodeDc(dc.Red, dc.Green, dc.Blue).EncodeBase83(resultBuffer.Slice(2, 4));

		for (var yComponent = 0; yComponent < componentsY; yComponent++)
		for (var xComponent = 0; xComponent < componentsX; xComponent++)
		{
			// Ignore DC component
			if (xComponent == 0 && yComponent == 0) continue;

			var factorIndex = componentsX * yComponent + xComponent;

			EncodeAc(factors[factorIndex].Red, factors[factorIndex].Green, factors[factorIndex].Blue, maximumValue)
				.EncodeBase83(resultBuffer.Slice(6 + (factorIndex - 1) * 2, 2));
		}

		return resultBuffer.ToString();
	}

	private static int EncodeAc(double r, double g, double b, double maximumValue)
	{
		var quantizedR = (int)Math.Max(0, Math.Min(18, Math.Floor(SignPow(r / maximumValue, 0.5) * 9 + 9.5)));
		var quantizedG = (int)Math.Max(0, Math.Min(18, Math.Floor(SignPow(g / maximumValue, 0.5) * 9 + 9.5)));
		var quantizedB = (int)Math.Max(0, Math.Min(18, Math.Floor(SignPow(b / maximumValue, 0.5) * 9 + 9.5)));

		return quantizedR * 19 * 19 + quantizedG * 19 + quantizedB;
	}

	private static int EncodeDc(double r, double g, double b)
	{
		var roundedR = LinearTosRgb(r);
		var roundedG = LinearTosRgb(g);
		var roundedB = LinearTosRgb(b);
		return (roundedR << 16) + (roundedG << 8) + roundedB;
	}

	private static void EncodeBase83(this int number, Span<char> output)
	{
		var length = output.Length;
		for (var index1 = 0; index1 < length; ++index1)
		{
			var index2 = number % 83;
			number /= 83;
			output[length - index1 - 1] =
				"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#$%*+,-.:;=?@[]^_{|}~"[index2];
		}
	}

	private static float SRgbToLinear(int value)
	{
		var num = value / (float)byte.MaxValue;
		return (float)(num <= 0.04045 ? num / 12.92 : float.Pow((num + 0.055f) / 1.055f, 2.4f));
	}

	private static int LinearTosRgb(double value)
	{
		var v = Math.Max(0.0, Math.Min(1.0, value));
		if (v <= 0.0031308) return (int)(v * 12.92 * 255 + 0.5);
		return (int)((1.055 * Math.Pow(v, 1 / 2.4) - 0.055) * 255 + 0.5);
	}

	private static double SignPow(double @base, double exponent)
	{
		return Math.Sign(@base) * Math.Pow(Math.Abs(@base), exponent);
	}

	private struct Pixel(double red, double green, double blue)
	{
		public double Red   = red;
		public double Green = green;
		public double Blue  = blue;
	}
}