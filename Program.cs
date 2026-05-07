using System;
using System.Collections.Generic;
using System.Linq;

namespace mkr1kpz
{
    public abstract class LightNode : IEnumerable<LightNode>
    {
        public abstract string OuterHtml();
        public abstract string InnerHtml();

        public virtual IEnumerator<LightNode> GetEnumerator()
        {
            // Default to Depth First Search
            return new HtmlDepthIterator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
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
            OnCreated();
        }

        // Template method hooks
        protected virtual void OnCreated() { }
        protected virtual void OnInserted(LightNode node) { }
        protected virtual void OnClassListApplied() { }

        public virtual int ChildrenCount => _children.Count;
        public virtual string Tag => _tag;
        public virtual DisplayType Display => _displayType;
        public virtual ClosingType Closing => _closingType;
        public IReadOnlyList<LightNode> Children => _children;

        public virtual void AddClass(string cssClass)
        {
            _cssClasses.Add(cssClass);
            OnClassListApplied();
        }

        public virtual void AddChild(LightNode node)
        {
            _children.Add(node);
            OnInserted(node);
        }
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

    // Example of using the Template Method
    public class CustomElementNode : LightElementNode
    {
        public CustomElementNode(string tag, DisplayType displayType, ClosingType closingType) 
            : base(tag, displayType, closingType) { }

        protected override void OnCreated()
        {
            Console.WriteLine($"[Hook] Element <{_tag}> was created.");
        }

        protected override void OnInserted(LightNode node)
        {
            Console.WriteLine($"[Hook] Node inserted into <{_tag}>.");
        }

        protected override void OnClassListApplied()
        {
            Console.WriteLine($"[Hook] Classes applied to <{_tag}>: {string.Join(", ", _cssClasses)}");
        }
    }

    public class HtmlDepthIterator : IEnumerator<LightNode>
    {
        private readonly LightNode _root;
        private LightNode _current;
        private Stack<LightNode> _stack;

        public HtmlDepthIterator(LightNode root)
        {
            _root = root;
            Reset();
        }

        public LightNode Current => _current;

        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_stack.Count == 0) return false;

            _current = _stack.Pop();

            if (_current is LightElementNode element)
            {
                // Push children in reverse order so they are processed left-to-right
                for (int i = element.Children.Count - 1; i >= 0; i--)
                {
                    _stack.Push(element.Children[i]);
                }
            }

            return true;
        }

        public void Reset()
        {
            _stack = new Stack<LightNode>();
            _stack.Push(_root);
            _current = null;
        }

        public void Dispose() { }
    }

    public class HtmlBreadthIterator : IEnumerator<LightNode>
    {
        private readonly LightNode _root;
        private LightNode _current;
        private Queue<LightNode> _queue;

        public HtmlBreadthIterator(LightNode root)
        {
            _root = root;
            Reset();
        }

        public LightNode Current => _current;

        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_queue.Count == 0) return false;

            _current = _queue.Dequeue();

            if (_current is LightElementNode element)
            {
                foreach (var child in element.Children)
                {
                    _queue.Enqueue(child);
                }
            }

            return true;
        }

        public void Reset()
        {
            _queue = new Queue<LightNode>();
            _queue.Enqueue(_root);
            _current = null;
        }

        public void Dispose() { }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("MKR 1 - Design Patterns in LightHTML\n");
            
            Console.WriteLine("--- Template Method ---");
            var customDiv = new CustomElementNode("div", DisplayType.Block, ClosingType.WithClosingTag);
            customDiv.AddClass("container");
            customDiv.AddClass("highlight");
            customDiv.AddChild(new LightTextNode("Text inside custom node"));
            
            Console.WriteLine("\nFinal HTML:");
            Console.WriteLine(customDiv.OuterHtml());

            Console.WriteLine("\n--- Iterator Pattern ---");
            var tree = new LightElementNode("html", DisplayType.Block, ClosingType.WithClosingTag);
            var head = new LightElementNode("head", DisplayType.Block, ClosingType.WithClosingTag);
            var body = new LightElementNode("body", DisplayType.Block, ClosingType.WithClosingTag);
            
            tree.AddChild(head);
            tree.AddChild(body);
            
            body.AddChild(new LightElementNode("h1", DisplayType.Block, ClosingType.WithClosingTag));
            body.AddChild(new LightElementNode("p", DisplayType.Block, ClosingType.WithClosingTag));

            Console.WriteLine("Depth-First Search (Default foreach):");
            foreach (var node in tree)
            {
                if (node is LightElementNode el) Console.WriteLine($"Found element: {el.Tag}");
                else Console.WriteLine("Found text node");
            }

            Console.WriteLine("\nBreadth-First Search:");
            var bfs = new HtmlBreadthIterator(tree);
            while (bfs.MoveNext())
            {
                if (bfs.Current is LightElementNode el) Console.WriteLine($"Found element: {el.Tag}");
                else Console.WriteLine("Found text node");
            }
        }
    }
}
