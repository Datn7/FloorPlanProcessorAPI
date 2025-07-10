using OpenCvSharp;
using System.Xml.Linq;

public class FloorPlanProcessorService
{
    public string ProcessAndGenerateSvg(string inputFilePath, string outputSvgPath)
    {
        using var src = Cv2.ImRead(inputFilePath);
        using var gray = new Mat();
        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
        using var edges = new Mat();
        Cv2.Canny(gray, edges, 50, 150);

        var lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 100, 50, 10);

        // Generate SVG
        XNamespace ns = "http://www.w3.org/2000/svg";
        var svg = new XElement(ns + "svg",
            new XAttribute("width", src.Width),
            new XAttribute("height", src.Height));

        if (lines != null)
        {
            foreach (var line in lines)
            {
                var x1 = line.P1.X;
                var y1 = line.P1.Y;
                var x2 = line.P2.X;
                var y2 = line.P2.Y;

                var lineElement = new XElement(ns + "line",
                    new XAttribute("x1", x1),
                    new XAttribute("y1", y1),
                    new XAttribute("x2", x2),
                    new XAttribute("y2", y2),
                    new XAttribute("stroke", "black"),
                    new XAttribute("stroke-width", "2"));
                svg.Add(lineElement);
            }
        }

        var svgDoc = new XDocument(svg);
        svgDoc.Save(outputSvgPath);

        return outputSvgPath;
    }
}
