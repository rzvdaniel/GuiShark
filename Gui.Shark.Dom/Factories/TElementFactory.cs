﻿using AngleSharp.Dom;
using Gui.Shark.Dom;
using Gui.Shark.Dom.Factories;
using System;
using System.Collections.Generic;

namespace AngleSharp.Services.Default
{
    /// <summary>
    /// Provides string to HTMLElement instance creation mappings.
    /// </summary>
    public class TElementFactory : IElementFactory<IElement>
    {
        private delegate TElement Creator();

        private Dictionary<string, Creator> creators;

        public TElement Create(IElement htmlElement, TElement parent)
        {
            creators = new Dictionary<string, Creator>(StringComparer.OrdinalIgnoreCase)
            {
                { "HtmlBodyElement", () => new TBody(htmlElement, parent) },
                { "HtmlDivElement", () => new TDiv(htmlElement, parent) },
                { "HtmlParagraphElement", () => new TParagraph(htmlElement, parent) },
                { "HtmlSpanElement", () => new TSpan(htmlElement, parent) },
            };

            var type = htmlElement.GetType();
            var creator = default(Creator);

            if (creators.TryGetValue(type.Name, out creator))
            {
                return creator();
            }

            // Default to span element 
            creators.TryGetValue("HtmlSpanElement", out creator);
            return creator();
        }
    }
}
