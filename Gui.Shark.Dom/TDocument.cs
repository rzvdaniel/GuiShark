﻿using AngleSharp;
using AngleSharp.Parser.Html;
using AngleSharp.Services.Default;
using Gui.Shark.Dom.Interfaces;
using System;

namespace Gui.Shark.Dom
{
    public class TDocument : IDocument
    {
        public IElement Body { get; set; }

        public TDocument()  {}

        public void Parse(string html)
        {
            var config = new Configuration().WithCss();
            var parser = new HtmlParser(config ?? Configuration.Default);
            var document = parser.Parse(html);

            var factory = new TElementFactory();
            Body = factory.Create(document.Body, null);
        }

        public void Render()
        {
            throw new NotImplementedException();
        }
    }
}
