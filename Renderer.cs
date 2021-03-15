using Svg;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace MathTex.Parser {

    public class Renderer {

        private static T Clamp<T>(T val, T min, T max) where T : IComparable<T> {
            if(val.CompareTo(min) < 0)
                return min;
            else if(val.CompareTo(max) > 0)
                return max;
            else
                return val;
        }

        /// <summary>
        /// Convert latex to image.
        /// </summary>
        /// <param name="latex"></param>
        /// <param name="err"></param>
        /// <param name="svgtext"></param>
        /// <param name="scale"></param>
        /// <param name="color"></param>
        /// <param name="dpi"></param>
        /// <returns></returns>
        public static Bitmap ConvertFormulaEX(string latex, out string err, out string svgtext, double scale = 1.0, 
            Color? color = null, int dpi = 300) {

            err = null;
            svgtext = null;

            try {
                // Get the formula latex.
                var tex = latex.Trim();
                if(Regex.IsMatch(tex, @"^\s*$")) {
                    err = "[ERROR] Empty formula.";
                    return null;
                }

                // Implement mathjax parser.
                svgtext = MathjaxParser.GetInstance().Run(tex, out err);
                if(svgtext is  null) {
                    return null;
                }

                {
                    var svgdoc = SvgDocument.FromSvg<SvgDocument>(svgtext);
                    svgdoc.Width *= (float)scale;
                    svgdoc.Height *= (float)(scale * 1.05);
                    svgdoc.Ppi = dpi;

                    // Render image.
                    var bitmap = new Bitmap((int)Math.Round(svgdoc.Width), (int)Math.Round(svgdoc.Height));
                    bitmap.SetResolution(dpi, dpi);
                    using(var graphic = Graphics.FromImage(bitmap)) {
                        Color col = (color != null && color.HasValue) ? color.Value : Color.Transparent;
                        graphic.Clear(col);
                        svgdoc.Draw(graphic);
                    }
                    return bitmap;
                }

            } catch(Exception e) {
                err = e.Message;
            }
            return null;
        }
    }
}
