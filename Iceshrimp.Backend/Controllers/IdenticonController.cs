using System.IO.Hashing;
using System.Text;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[EnableCors("drive")]
[Route("/identicon/{id}")]
public class IdenticonController : Controller {
	public IActionResult GetIdenticon(string id) {
		using var bitmap = new SkiaBitmapExportContext(Size, Size, 1.0f);

		var canvas = bitmap.Canvas;
		var random = new Random((int)XxHash32.HashToUInt32(Encoding.UTF8.GetBytes(id)));
		var color  = Colors[random.Next(0, Colors.Count - 1)];

		var gradient = new LinearGradientPaint(new Point(0, 0), new Point(1, 1)) {
			StartColor = color.start,
			EndColor   = color.end
		};

		var rect = new RectF(0, 0, Size, Size);
		canvas.SetFillPaint(gradient, rect);
		canvas.FillRectangle(rect);

		var paint = new SolidPaint(Microsoft.Maui.Graphics.Colors.White);
		canvas.SetFillPaint(paint, rect);

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

				canvas.FillRectangle(new Rect(actualX, actualY, CellSize, CellSize));
			}
		}

		var stream = new MemoryStream();
		bitmap.WriteToStream(stream);
		stream.Seek(0, SeekOrigin.Begin);

		Response.Headers.CacheControl = "max-age=31536000, immutable";
		return File(stream, "image/png");
	}

	#region Color definitions & Constants

	private const int Size           = 160;
	private const int Margin         = Size / 4;
	private const int CellCount      = 5;
	private const int CellCanvasSize = Size - Margin * 2;
	private const int CellSize       = CellCanvasSize / CellCount;
	private const int SideN          = CellCount / 2;

	private static readonly List<(Color start, Color end)> Colors = [
		(new Color(235, 111, 146), new Color(180, 99, 122)),
		(new Color(246, 193, 119), new Color(234, 157, 52)),
		(new Color(235, 188, 186), new Color(215, 130, 126)),
		(new Color(156, 207, 216), new Color(86, 148, 159)),
		(new Color(196, 167, 231), new Color(144, 122, 169)),
		(new Color(235, 111, 146), new Color(246, 193, 119)),
		(new Color(235, 111, 146), new Color(235, 188, 186)),
		(new Color(235, 111, 146), new Color(49, 116, 143)),
		(new Color(235, 111, 146), new Color(156, 207, 216)),
		(new Color(235, 111, 146), new Color(196, 167, 231)),
		(new Color(246, 193, 119), new Color(235, 111, 146)),
		(new Color(246, 193, 119), new Color(235, 188, 186)),
		(new Color(246, 193, 119), new Color(49, 116, 143)),
		(new Color(246, 193, 119), new Color(156, 207, 216)),
		(new Color(246, 193, 119), new Color(196, 167, 231)),
		(new Color(235, 188, 186), new Color(235, 111, 146)),
		(new Color(235, 188, 186), new Color(246, 193, 119)),
		(new Color(235, 188, 186), new Color(49, 116, 143)),
		(new Color(235, 188, 186), new Color(156, 207, 216)),
		(new Color(235, 188, 186), new Color(196, 167, 231)),
		(new Color(49, 116, 143), new Color(235, 111, 146)),
		(new Color(49, 116, 143), new Color(246, 193, 119)),
		(new Color(49, 116, 143), new Color(235, 188, 186)),
		(new Color(49, 116, 143), new Color(156, 207, 216)),
		(new Color(49, 116, 143), new Color(196, 167, 231)),
		(new Color(156, 207, 216), new Color(235, 111, 146)),
		(new Color(156, 207, 216), new Color(246, 193, 119)),
		(new Color(156, 207, 216), new Color(235, 188, 186)),
		(new Color(156, 207, 216), new Color(49, 116, 143)),
		(new Color(156, 207, 216), new Color(196, 167, 231)),
		(new Color(196, 167, 231), new Color(235, 111, 146)),
		(new Color(196, 167, 231), new Color(246, 193, 119)),
		(new Color(196, 167, 231), new Color(235, 188, 186)),
		(new Color(196, 167, 231), new Color(49, 116, 143)),
		(new Color(196, 167, 231), new Color(156, 207, 216))
	];

	#endregion
}