using System.IO.Hashing;
using System.Text;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[EnableCors("drive")]
[Route("/identicon/{id}")]
public class IdenticonController : Controller {
	[HttpGet]
	public async Task GetIdenticon(string id) {
		using var image = new Image<Rgb24>(Size, Size);

		var random = new Random((int)XxHash32.HashToUInt32(Encoding.UTF8.GetBytes(id)));
		var color  = Colors[random.Next(0, Colors.Count - 1)];

		var gradient = new LinearGradientBrush(new Point(0, 0), new Point(Size, Size), GradientRepetitionMode.None,
		                                       new ColorStop(0, color.start), new ColorStop(1, color.end));


		image.Mutate(p => p.Fill(gradient));

		var paint = new SolidBrush(Color.White);

		var side   = new bool[SideN, CellCount];
		var center = new bool[CellCount];

		for (var i = 0; i < SideN; i++) {
			for (var j = 0; j < CellCount; j++) {
				side[i, j] = random.Next(3) == 0;
			}
		}

		for (var i = 0; i < CellCount; i++) {
			center[i] = random.Next(3) == 0;
		}

		for (var i = 0; i < CellCount; i++) {
			for (var j = 0; j < CellCount; j++) {
				var isXCenter = i == (CellCount - 1) / 2;
				if (isXCenter && !center[j])
					continue;

				var isLeftSide = i < (CellCount - 1) / 2;
				if (isLeftSide && !side[i, j])
					continue;

				var isRightSide = i > (CellCount - 1) / 2;
				if (isRightSide && !side[SideN - (i - SideN), j])
					continue;

				var actualX = Margin + CellSize * i;
				var actualY = Margin + CellSize * j;

				image.Mutate(p => p.Fill(paint, new Rectangle(actualX, actualY, CellSize, CellSize)));
			}
		}

		Response.Headers.CacheControl = "max-age=31536000, immutable";
		Response.Headers.ContentType  = "image/png";
		await image.SaveAsPngAsync(Response.Body);
	}

	#region Color definitions & Constants

	private const int Size           = 160;
	private const int Margin         = Size / 4;
	private const int CellCount      = 5;
	private const int CellCanvasSize = Size - Margin * 2;
	private const int CellSize       = CellCanvasSize / CellCount;
	private const int SideN          = CellCount / 2;

	private static readonly List<(Color start, Color end)> Colors = [
		(new Color(new Rgb24(235, 111, 146)), new Color(new Rgb24(180, 99, 122))),
		(new Color(new Rgb24(246, 193, 119)), new Color(new Rgb24(234, 157, 52))),
		(new Color(new Rgb24(235, 188, 186)), new Color(new Rgb24(215, 130, 126))),
		(new Color(new Rgb24(156, 207, 216)), new Color(new Rgb24(86, 148, 159))),
		(new Color(new Rgb24(196, 167, 231)), new Color(new Rgb24(144, 122, 169))),
		(new Color(new Rgb24(235, 111, 146)), new Color(new Rgb24(246, 193, 119))),
		(new Color(new Rgb24(235, 111, 146)), new Color(new Rgb24(235, 188, 186))),
		(new Color(new Rgb24(235, 111, 146)), new Color(new Rgb24(49, 116, 143))),
		(new Color(new Rgb24(235, 111, 146)), new Color(new Rgb24(156, 207, 216))),
		(new Color(new Rgb24(235, 111, 146)), new Color(new Rgb24(196, 167, 231))),
		(new Color(new Rgb24(246, 193, 119)), new Color(new Rgb24(235, 111, 146))),
		(new Color(new Rgb24(246, 193, 119)), new Color(new Rgb24(235, 188, 186))),
		(new Color(new Rgb24(246, 193, 119)), new Color(new Rgb24(49, 116, 143))),
		(new Color(new Rgb24(246, 193, 119)), new Color(new Rgb24(156, 207, 216))),
		(new Color(new Rgb24(246, 193, 119)), new Color(new Rgb24(196, 167, 231))),
		(new Color(new Rgb24(235, 188, 186)), new Color(new Rgb24(235, 111, 146))),
		(new Color(new Rgb24(235, 188, 186)), new Color(new Rgb24(246, 193, 119))),
		(new Color(new Rgb24(235, 188, 186)), new Color(new Rgb24(49, 116, 143))),
		(new Color(new Rgb24(235, 188, 186)), new Color(new Rgb24(156, 207, 216))),
		(new Color(new Rgb24(235, 188, 186)), new Color(new Rgb24(196, 167, 231))),
		(new Color(new Rgb24(49, 116, 143)), new Color(new Rgb24(235, 111, 146))),
		(new Color(new Rgb24(49, 116, 143)), new Color(new Rgb24(246, 193, 119))),
		(new Color(new Rgb24(49, 116, 143)), new Color(new Rgb24(235, 188, 186))),
		(new Color(new Rgb24(49, 116, 143)), new Color(new Rgb24(156, 207, 216))),
		(new Color(new Rgb24(49, 116, 143)), new Color(new Rgb24(196, 167, 231))),
		(new Color(new Rgb24(156, 207, 216)), new Color(new Rgb24(235, 111, 146))),
		(new Color(new Rgb24(156, 207, 216)), new Color(new Rgb24(246, 193, 119))),
		(new Color(new Rgb24(156, 207, 216)), new Color(new Rgb24(235, 188, 186))),
		(new Color(new Rgb24(156, 207, 216)), new Color(new Rgb24(49, 116, 143))),
		(new Color(new Rgb24(156, 207, 216)), new Color(new Rgb24(196, 167, 231))),
		(new Color(new Rgb24(196, 167, 231)), new Color(new Rgb24(235, 111, 146))),
		(new Color(new Rgb24(196, 167, 231)), new Color(new Rgb24(246, 193, 119))),
		(new Color(new Rgb24(196, 167, 231)), new Color(new Rgb24(235, 188, 186))),
		(new Color(new Rgb24(196, 167, 231)), new Color(new Rgb24(49, 116, 143))),
		(new Color(new Rgb24(196, 167, 231)), new Color(new Rgb24(156, 207, 216)))
	];

	#endregion
}