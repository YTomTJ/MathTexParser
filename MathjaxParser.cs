using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using JavaScriptEngineSwitcher.ChakraCore;

namespace MathTex.Parser {

    public class MathjaxParser {

        private static string _ErrHead = "data-mjx-error";
        private static ChakraCoreJsEngine _MathjaxEngine;
        private static bool _IsLoaded { get; set; }

        //TODO: How to get error to not show in svg.
        // loader: { load: ['[tex]/unicode'] },
        // tex: { packages: {'[+]': ['unicode']} },
        private static string _InitSettings = @"MathJax = {
    startup: { typeset: false },
    tex: { packages: { '[-]': ['noerrors'] } }
};";

        private static string _ConvertFunction = @"
function HtmlToSvgConvert(text, options=null) {
    MathJax.texReset();
    options==null && (options={ em:'16', scale:'1.0' });
    options.display==null && (options.display = true);
    var node = MathJax.tex2svg(text, options);
    return MathJax.startup.adaptor.outerHTML(node.children[0]);
};
function HtmlToMMLConvert(text, options=null) {
    MathJax.texReset();
    options==null && (options={ em:'16', scale:'1.0' });
    options.display==null && (options.display = true);
    return MathJax.tex2mml(text, options);
};";


        private static MathjaxParser _Instance = null;

        private MathjaxParser() {
            Load();
        }

        public static MathjaxParser GetInstance() {
            if (_Instance is null)
                _Instance = new MathjaxParser();
            return _Instance;
        }

        public bool IsLoaded { get => _IsLoaded; }


        /// <summary>
        /// Parse for latex to svg.
        /// </summary>
        /// <param name="latex">Source latex formula text.</param>
        /// <param name="options">Options for MathJax conversion(in JS way). 
        ///     NOTE that string object should add additinal quotation marks(' or ").</param>
        /// <returns></returns>
        public string Run(string latex, out string err, Tuple<string, string>[] options = null) {

            if (_MathjaxEngine is null) {
                err = "Mathjax engine is null.";
                return null;
            }

            if (!_IsLoaded) {
                err = "Module is not loaded or loaded failed.";
                return null;
            }
            // Preprocess formula text
            latex = latex.Trim();
            latex = Regex.Replace(latex, @"[\r\n]", " ");
            latex = Regex.Replace(latex, @"\\", "\\\\");
            var argument = GetOptions(options);

            // Make mathjax call expression
            var exprsvg = $"HtmlToSvgConvert(\"{latex}\",{argument})";
            var result = _MathjaxEngine.Evaluate<string>(exprsvg);

            err = CheckError(result);
            return (err is null) ? result : null;
        }

        /// <summary>
        /// Parse for latex to svg.
        /// </summary>
        /// <param name="latex">Source latex formula text.</param>
        /// <param name="options">Options for MathJax conversion(in JS way). 
        ///     NOTE that string object should add additinal quotation marks(' or ").</param>
        /// <returns></returns>
        private string RunMML(string latex, Tuple<string, string>[] options = null) {

            if (_MathjaxEngine is null)
                return null;

            if (!_IsLoaded) {
                return null;
            }
            // Preprocess formula text
            latex = latex.Trim();
            latex = Regex.Replace(latex, @"[\r\n]", " ");
            latex = Regex.Replace(latex, @"\\", "\\\\");
            var argument = GetOptions(options);

            // Make mathjax call expression
            var exprmml = $"HtmlToMMLConvert(\"{latex}\",{argument})";
            return _MathjaxEngine.Evaluate<string>(exprmml);
        }

        /// <summary>
        /// Parse for latex to svg and mml.
        /// </summary>
        /// <param name="latex">Source latex formula text.</param>
        /// <param name="mml">Output mml text.</param>
        /// <param name="options">Options for MathJax conversion(in JS way). 
        ///     NOTE that string object should add additinal quotation marks(' or ").</param>
        /// <returns></returns>
        public Tuple<string, string> RunEx(string latex, out string err, Tuple<string, string>[] options = null) {

            if (_MathjaxEngine is null) {
                err = "Mathjax engine is null.";
                return null;
            }

            if (!_IsLoaded) {
                err = "Module is not loaded or loaded failed.";
                return null;
            }

            var svg = Run(latex, out err, options);
            if (svg is null) {
                return null;
            } else {
                var mml = RunMML(latex, options);
                return new Tuple<string, string>(svg, mml);
            }
        }

        private string CheckError(string text) {
            int ierr = text.IndexOf(_ErrHead);
            if (ierr >= 0) {
                int ist = text.IndexOf('\"', ierr + _ErrHead.Length) + 1;
                int ied = text.IndexOf('\"', ist + 1);
                return text.Substring(ist, ied - ist) + ".";
            }
            return null;
        }

        private static string GetOptions(Tuple<string, string>[] options) {
            // Parse options
            // TODO: not functinal options
            if (options is null) {
                options = new Tuple<string, string>[] {
                    //new Tuple<string, string>("display", "true"),
                    //new Tuple<string, string>("em", "\'20\'"),
                    //new Tuple<string, string>("family", "\'\'"),
                    //new Tuple<string, string>("scale", "\'1.0\'"),
                };
            }
            var args = new List<string>();
            foreach (var op in options) {
                args.Add($"{op.Item1}:{op.Item2}");
            }
            var argument = $"{{{string.Join<string>(",", args)}}}";
            return argument;
        }

        /// <summary>
        /// Load JS engine and execute js resource.
        /// </summary>
        private static void Load() {
            if (!_IsLoaded) {
                try {
                    _MathjaxEngine = new ChakraCoreJsEngine();

                    _MathjaxEngine.Execute(_InitSettings);
                    _MathjaxEngine.Execute(Resources.tex_svg_full);
                    _MathjaxEngine.Execute(Resources.liteDOM);
                    _MathjaxEngine.Execute("MathJax.config.startup.ready();");
                    _MathjaxEngine.Execute(_ConvertFunction);
                    _IsLoaded = true;
                } catch (Exception e) {
                    _IsLoaded = false;
                    throw e;
                }
            }
        }

        /// <summary>
        /// Dispose JS engine resource.
        /// </summary>
        public void UnLoad() {
            if (_MathjaxEngine != null) {
                _MathjaxEngine.Dispose();
                _MathjaxEngine = null;
            }
            _IsLoaded = false;
        }
    }
}
