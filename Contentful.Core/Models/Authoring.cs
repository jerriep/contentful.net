﻿using Contentful.Core.Models.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Contentful.Core.Models
{
    public class Document
    {
        public string NodeType { get; set; }
        public string NodeClass { get; set; }
        public List<IContent> Content { get; set; }
    }

    public class Text : IContent
    {
        public string NodeType { get; set; }
        public string NodeClass { get; set; }
        public string Value { get; set; }
        public List<Mark> Marks { get; set; }
    }

    public class Hyperlink : IContent
    {
        public string NodeType { get; set; }
        public string NodeClass { get; set; }
        public HyperlinkData Data { get; set; }
        public List<IContent> Content { get; set; }
    }

    public class HyperlinkData
    {
        public string Url { get; set; }
        public string Title { get; set; }
    }

    public class Mark : IContent
    {
        public string Type { get; set; }
    }

    public class Paragraph : IContent
    {
        public string NodeType { get; set; }
        public string NodeClass { get; set; }
        public List<IContent> Content { get; set; }
    }

    public class Heading : IContent
    {
        public string NodeType { get; set; }
        public string NodeClass { get; set; }
        public int HeadingSize { get; set; }
        public List<IContent> Content { get; set; }
    }

    public class Block : IContent
    {
        public string NodeType { get; set; }
        public string NodeClass { get; set; }
        public BlockData Data { get; set; }
    }

    public class BlockData
    {
        public ReferenceProperties Target { get; set; }
    }

    public interface IContent
    {

    }

    public class HtmlRenderer
    {
        private readonly ContentRenderererCollection _contentRenderererCollection;

        public HtmlRenderer()
        {
            _contentRenderererCollection = new ContentRenderererCollection();
            _contentRenderererCollection.AddRenderers(new List<IContentRenderer> {
                new ParagraphRenderer(_contentRenderererCollection),
                new HyperlinkContentRenderer(_contentRenderererCollection),
                new TextRenderer(),
                new HeadingRenderer(_contentRenderererCollection),
                new AssetRenderer(),
                new NullContentRenderer()
            });
        }

        public string ToHtml(Document doc)
        {
            var sb = new StringBuilder();
            foreach (var content in doc.Content)
            {
                var renderer = _contentRenderererCollection.GetRendererForContent(content);
                sb.Append(renderer.Render(content));
            }

            return sb.ToString();
        }

        public void AddRenderer(IContentRenderer renderer)
        {
            _contentRenderererCollection.AddRenderer(renderer);
        }
    }

    public class ContentRenderererCollection
    {
        readonly List<IContentRenderer> _renderers = new List<IContentRenderer>();
        public void AddRenderer(IContentRenderer renderer)
        {
            _renderers.Add(renderer);
        }

        public void AddRenderers(IEnumerable<IContentRenderer> collection)
        {
            _renderers.AddRange(collection);
        }

        public IContentRenderer GetRendererForContent(IContent content)
        {
            return _renderers.OrderBy(c => c.Order).FirstOrDefault(c => c.SupportsContent(content));
        }
    }

    public interface IContentRenderer
    {
        int Order { get; set; }
        bool SupportsContent(IContent content);
        string Render(IContent content);
    }

    public class ParagraphRenderer : IContentRenderer
    {
        private readonly ContentRenderererCollection _renderererCollection;

        public ParagraphRenderer(ContentRenderererCollection renderererCollection)
        {
            _renderererCollection = renderererCollection;
        }

        public int Order { get; set; } = 100;
        public bool SupportsContent(IContent content)
        {
            return content is Paragraph;
        }
        public string Render(IContent content)
        {
            var paragraph = content as Paragraph;
            var sb = new StringBuilder();
            sb.Append("<p>");

            foreach (var subContent in paragraph.Content)
            {
                var renderer = _renderererCollection.GetRendererForContent(subContent);
                sb.Append(renderer.Render(subContent));
            }

            sb.Append("</p>");
            return sb.ToString();
        }
    }

    public class HeadingRenderer : IContentRenderer
    {
        private readonly ContentRenderererCollection _renderererCollection;

        public HeadingRenderer(ContentRenderererCollection renderererCollection)
        {
            _renderererCollection = renderererCollection;
        }

        public int Order { get; set; } = 100;
        public bool SupportsContent(IContent content)
        {
            return content is Heading;
        }
        public string Render(IContent content)
        {
            var heading = content as Heading;
            var sb = new StringBuilder();
            sb.Append($"<h{heading.HeadingSize}>");

            foreach (var subContent in heading.Content)
            {
                var renderer = _renderererCollection.GetRendererForContent(subContent);
                sb.Append(renderer.Render(subContent));
            }

            sb.Append($"</h{heading.HeadingSize}>");
            return sb.ToString();
        }
    }

    public class TextRenderer : IContentRenderer
    {
        public int Order { get; set; } = 100;

        public bool SupportsContent(IContent content)
        {
            return content is Text;
        }

        public string Render(IContent content)
        {
            var text = content as Text;
            var sb = new StringBuilder();

            if (text.Marks != null)
            {
                foreach (var mark in text.Marks)
                {
                    sb.Append($"<{MarkToHtmlTag(mark)}>");
                }
            }

            sb.Append(text.Value);

            if (text.Marks != null)
            {
                foreach (var mark in text.Marks)
                {
                    sb.Append($"</{MarkToHtmlTag(mark)}>");
                }
            }

            return sb.ToString();
        }

        private string MarkToHtmlTag(Mark mark)
        {
            switch (mark.Type)
            {
                case "bold":
                    return "strong";
                case "underline":
                    return "u";
                case "italic":
                    return "em";
            }

            return "span";
        }
    }

    public class AssetRenderer : IContentRenderer
    {
        public int Order { get; set; } = 100;

        public bool SupportsContent(IContent content)
        {
            return content is Asset;
        }

        public string Render(IContent content)
        {
            var asset = content as Asset;
            var sb = new StringBuilder();
            if(asset.File?.ContentType != null && asset.File.ContentType.ToLower().Contains("image"))
            {
                sb.Append($"<img src=\"{asset.File.Url}\" alt=\"{asset.Title}\" />");
            }else
            {
                sb.Append($"<a href=\"{asset.File.Url}\">{asset.Title}</a>");
            }

            return sb.ToString();
        }
    }

    public class HyperlinkContentRenderer : IContentRenderer
    {
        private readonly ContentRenderererCollection _renderererCollection;

        public HyperlinkContentRenderer(ContentRenderererCollection contentRenderererCollection)
        {
            _renderererCollection = contentRenderererCollection;
        }

        public int Order { get; set; } = 100;

        public bool SupportsContent(IContent content)
        {
            return content is Hyperlink;
        }

        public string Render(IContent content)
        {
            var link = content as Hyperlink;
            var sb = new StringBuilder();

            sb.Append($"<a href=\"{link.Data.Url}\" title=\"{link.Data.Title}\">");

            foreach (var subContent in link.Content)
            {
                var renderer = _renderererCollection.GetRendererForContent(subContent);
                sb.Append(renderer.Render(subContent));
            }

            sb.Append("</a>");

            return sb.ToString();
        }
    }

    public class NullContentRenderer : IContentRenderer
    {
        public int Order { get; set; } = 100;
        public string Render(IContent content)
        {
            return "";
        }
        public bool SupportsContent(IContent content)
        {
            return true;
        }
    }
}
