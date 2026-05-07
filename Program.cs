using System;
using System.Collections.Generic;
using System.Linq;

namespace mkr1kpz
{
    public abstract class LightNode
    {
        public abstract string OuterHtml();
        public abstract string InnerHtml();
    }

    public class LightTextNode : LightNode
    {
        private readonly string _text;

        public LightTextNode(string text)
        {
            _text = text;
        }

        public override string OuterHtml() => _text;
        public override string InnerHtml() => _text;
    }

    public enum DisplayType { Block, Inline }
    public enum ClosingType { SelfClosing, WithClosingTag }

    public class LightElementNode : LightNode
    {
        protected string _tag;
        protected DisplayType _displayType;
        protected ClosingType _closingType;
        protected List<string> _cssClasses = new();
        protected List<LightNode> _children = new();

        public LightElementNode(string tag, DisplayType displayType, ClosingType closingType)
        {
            _tag = tag;
            _displayType = displayType;
            _closingType = closingType;
        }

        public virtual int ChildrenCount => _children.Count;
        public virtual string Tag => _tag;
        public virtual DisplayType Display => _displayType;
        public virtual ClosingType Closing => _closingType;
        public IReadOnlyList<LightNode> Children => _children;

        public virtual void AddClass(string cssClass) => _cssClasses.Add(cssClass);
        public virtual void AddChild(LightNode node) => _children.Add(node);
        public virtual void RemoveChild(LightNode node) => _children.Remove(node);

        public override string InnerHtml() => string.Concat(_children.Select(c => c.OuterHtml()));

        public override string OuterHtml()
        {
            var classes = _cssClasses.Count == 0 ? "" : $" class=\"{string.Join(" ", _cssClasses)}\"";
            if (Closing == ClosingType.SelfClosing)
            {
                return $"<{Tag}{classes}/>";
            }

            return $"<{Tag}{classes}>{InnerHtml()}</{Tag}>";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("MKR 1 - Design Patterns in LightHTML");
            
            var div = new LightElementNode("div", DisplayType.Block, ClosingType.WithClosingTag);
            div.AddClass("container");
            div.AddChild(new LightTextNode("Hello, world!"));
            
            Console.WriteLine(div.OuterHtml());
        }
    }
}
