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

        public INodeState NodeState { get; set; }

        public LightElementNode(string tag, DisplayType displayType, ClosingType closingType)
        {
            _tag = tag;
            _displayType = displayType;
            _closingType = closingType;
            NodeState = new ExpandedNodeState();
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

        public virtual void RemoveClass(string cssClass)
        {
            _cssClasses.Remove(cssClass);
            OnClassListApplied();
        }

        public override string InnerHtml() => NodeState.RenderInnerHtml(this);

        // Utility to get base InnerHtml
        public string BaseInnerHtml() => string.Concat(_children.Select(c => c.OuterHtml()));

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

    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    public class AddChildCommand : ICommand
    {
        private readonly LightElementNode _parent;
        private readonly LightNode _child;

        public AddChildCommand(LightElementNode parent, LightNode child)
        {
            _parent = parent;
            _child = child;
        }

        public void Execute() => _parent.AddChild(_child);
        public void Undo() => _parent.RemoveChild(_child);
    }

    public class AddClassCommand : ICommand
    {
        private readonly LightElementNode _element;
        private readonly string _cssClass;

        public AddClassCommand(LightElementNode element, string cssClass)
        {
            _element = element;
            _cssClass = cssClass;
        }

        public void Execute() => _element.AddClass(_cssClass);
        public void Undo() => _element.RemoveClass(_cssClass);
    }

    public class HtmlCommandManager
    {
        private readonly Stack<ICommand> _history = new();

        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _history.Push(command);
        }

        public void Undo()
        {
            if (_history.Count > 0)
            {
                var command = _history.Pop();
                command.Undo();
            }
        }
    }

    public interface INodeState
    {
        string RenderInnerHtml(LightElementNode node);
    }

    public class ExpandedNodeState : INodeState
    {
        public string RenderInnerHtml(LightElementNode node)
        {
            return node.BaseInnerHtml();
        }
    }

    public class CollapsedNodeState : INodeState
    {
        public string RenderInnerHtml(LightElementNode node)
        {
            return "...";
        }
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

            Console.WriteLine("\n--- Command Pattern ---");
            var manager = new HtmlCommandManager();
            var cmdDiv = new LightElementNode("div", DisplayType.Block, ClosingType.WithClosingTag);
            
            Console.WriteLine("Executing: Add class 'primary'");
            manager.ExecuteCommand(new AddClassCommand(cmdDiv, "primary"));
            
            var span = new LightElementNode("span", DisplayType.Inline, ClosingType.WithClosingTag);
            Console.WriteLine("Executing: Add child <span>");
            manager.ExecuteCommand(new AddChildCommand(cmdDiv, span));
            
            Console.WriteLine($"HTML before undo: {cmdDiv.OuterHtml()}");

            manager.Undo();
            Console.WriteLine($"HTML after 1st undo: {cmdDiv.OuterHtml()}");
            
            manager.Undo();
            Console.WriteLine($"HTML after 2nd undo: {cmdDiv.OuterHtml()}");

            Console.WriteLine("\n--- State Pattern ---");
            var stateDiv = new LightElementNode("div", DisplayType.Block, ClosingType.WithClosingTag);
            stateDiv.AddChild(new LightTextNode("This is some text inside the div"));
            stateDiv.AddChild(new LightElementNode("p", DisplayType.Block, ClosingType.WithClosingTag));
            
            Console.WriteLine("Expanded State:");
            Console.WriteLine(stateDiv.OuterHtml());
            
            Console.WriteLine("Changing state to Collapsed...");
            stateDiv.NodeState = new CollapsedNodeState();
            Console.WriteLine(stateDiv.OuterHtml());
        }
    }
}
