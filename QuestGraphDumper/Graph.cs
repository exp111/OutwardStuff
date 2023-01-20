using System;
using System.Collections.Generic;
using System.Xml.Serialization;

#pragma warning disable CS0618 // GoToNode is deprecated
namespace GraphDumper
{
    public class Graph
    {
        public List<Node> Nodes;
        [XmlIgnore]
        internal Dictionary<NodeCanvas.Framework.Node, int> NodeIDs;
        public Graph(NodeCanvas.Framework.Graph tree)
        {
            GraphDumper.DebugLog($"Creating graph from {tree}");
            Nodes = new();
            NodeIDs = new();
            var i = 0;
            foreach (var node in tree.allNodes)
            {
                GraphDumper.DebugLog($"{i}: {node}");
                Node newNode = node switch
                {
                    NodeCanvas.StateMachines.ActionState actionState => new ActionState(this, actionState, i),
                    NodeCanvas.StateMachines.ConcurrentState concurState => new ConcurrentState(this, concurState, i),
                    NodeCanvas.DialogueTrees.ActionNode actionNode => new ActionNode(this, actionNode, i),
                    NodeCanvas.DialogueTrees.FinishNode finishNode => new FinishNode(this, finishNode, i),
                    NodeCanvas.DialogueTrees.ConditionNode conditionNode => new ConditionNode(this, conditionNode, i),
                    NodeCanvas.DialogueTrees.GoToNode gotoNode => new GoToNode(this, gotoNode, i),
                    NodeCanvas.DialogueTrees.StatementNode statementNode => new StatementNode(this, statementNode, i),
                    NodeCanvas.DialogueTrees.StatementNodeExt statementNodeExt => new StatementNodeExt(this, statementNodeExt, i),
                    NodeCanvas.DialogueTrees.MultipleChoiceNode multiChoiceNode => new MultipleChoiceNode(this, multiChoiceNode, i),
                    NodeCanvas.DialogueTrees.MultipleChoiceNodeExt multiChoiceNodeExt => new MultipleChoiceNodeExt(this, multiChoiceNodeExt, i),
                    _ => new Node(this, node, i),
                };
                //TODO: include dialogue nodes
                // MultipleChoice node

                GraphDumper.DebugLog($"Created node {newNode}");
                Nodes.Add(newNode);
                NodeIDs.Add(node, i);
                i++;
            }

            // insert right ids into connections
            foreach (var node in Nodes)
            {
                node.Connect();
            }
        }

        public Graph()
        {
            GraphDumper.DebugLog($"Creating empty graph");
            Nodes = new();
            NodeIDs = new();
        }
    }

    //INFO: the xmlserializer needs to know all subtypes so we just xmlinclude them here
    [XmlInclude(typeof(FSMConnection))]
    public class Connection
    {
        public int TargetID;

        public Connection(Graph graph, NodeCanvas.Framework.Connection connection)
        {
            if (!graph.NodeIDs.TryGetValue(connection.targetNode, out var targetID))
                throw new Exception("TargetNode has no ID");
            TargetID = targetID;
        }

        public Connection()
        {
            GraphDumper.DebugLog($"Creating empty connection");
        }
    }

    public class FSMConnection : Connection
    {
        public string Condition;
        public FSMConnection(Graph graph, NodeCanvas.StateMachines.FSMConnection connection) : base(graph, connection)
        {
            var condition = "";
            if (connection.condition != null)
                condition = connection.condition.summaryInfo;
            Condition = condition;
        }

        public FSMConnection()
        {
            GraphDumper.DebugLog($"Creating empty fsm connection");
        }
    }

    //INFO: the xmlserializer needs to know all subtypes so we just xmlinclude them here
    [XmlInclude(typeof(ActionState))]
    [XmlInclude(typeof(ConcurrentState))]
    [XmlInclude(typeof(ActionNode))]
    [XmlInclude(typeof(ConditionNode))]
    [XmlInclude(typeof(FinishNode))]
    [XmlInclude(typeof(GoToNode))]
    [XmlInclude(typeof(StatementNode))]
    [XmlInclude(typeof(StatementNodeExt))]
    [XmlInclude(typeof(MultipleChoiceNode))]
    [XmlInclude(typeof(MultipleChoiceNodeExt))]
    public class Node
    {
        public int ID;
        public string Name;
        public List<Connection> Outgoing;
        [XmlIgnore]
        protected NodeCanvas.Framework.Node original;
        [XmlIgnore]
        protected Graph _graph;

        public Node(Graph graph, NodeCanvas.Framework.Node node, int id)
        {
            GraphDumper.DebugLog($"Creating node with original type of {node.GetType()}");
            Outgoing = new();
            ID = id;
            Name = node.name;

            original = node;
            _graph = graph;
        }

        public Node()
        {
            GraphDumper.DebugLog($"Creating empty node");
        }

        public virtual void Connect()
        {
            foreach (var outgoing in original.outConnections)
            {
                Connection connection = outgoing switch
                {
                    NodeCanvas.StateMachines.FSMConnection fsmConnection => new FSMConnection(_graph, fsmConnection),
                    _ => new Connection(_graph, outgoing),
                };
                Outgoing.Add(connection);
            }
        }

        //TODO: patch html tags away?
        public string Sanitize(string text)
        {
            //TODO: remove html tags?
            return text;
        }
    }

    public class ActionState : Node
    {
        //TODO: as list?
        public string ActionList;
        public ActionState(Graph graph, NodeCanvas.StateMachines.ActionState node, int ID) : base(graph, node, ID)
        {
            ActionList = "";
            if (node.actionList != null)
                ActionList = Sanitize(node.actionList.summaryInfo);
        }

        public ActionState()
        {
            GraphDumper.DebugLog($"Creating empty ActionState");
        }
    }

    public class ConcurrentState : Node
    {
        //TODO: do as lists?
        public string ConditionList;
        public string ActionList;
        public ConcurrentState(Graph graph, NodeCanvas.StateMachines.ConcurrentState node, int ID) : base(graph, node, ID)
        {
            //TODO: better stringify than this?
            ConditionList = "";
            if (node.conditionList != null)
            {
                ConditionList = Sanitize(node.conditionList.summaryInfo);
            }
            ActionList = "";
            if (node.actionList != null)
            {
                ActionList = Sanitize(node.actionList.summaryInfo);
            }
        }

        public ConcurrentState()
        {
            GraphDumper.DebugLog($"Creating empty ConcurrentState");
        }
    }

    public class ActionNode : Node
    {
        //TODO: as list?
        public string ActionList;
        public ActionNode(Graph graph, NodeCanvas.DialogueTrees.ActionNode node, int ID) : base(graph, node, ID)
        {
            ActionList = "";
            if (node.action != null)
                ActionList = Sanitize(node.action.summaryInfo);
        }

        public ActionNode()
        {
            GraphDumper.DebugLog($"Creating empty ActionNode");
        }
    }

    public class ConditionNode : Node
    {
        public string Condition;
        public ConditionNode(Graph graph, NodeCanvas.DialogueTrees.ConditionNode node, int ID) : base(graph, node, ID)
        {
            Condition = "";
            if (node.condition != null)
                Condition = Sanitize(node.condition.summaryInfo);
        }

        public ConditionNode()
        {
            GraphDumper.DebugLog($"Creating empty ConditionNode");
        }
    }

    public class FinishNode : Node
    {
        public string FinishState;
        public FinishNode(Graph graph, NodeCanvas.DialogueTrees.FinishNode node, int ID) : base(graph, node, ID)
        {
            FinishState = node.finishState.ToString();
        }

        public FinishNode()
        {
            GraphDumper.DebugLog($"Creating empty FinishNode");
        }
    }

    public class GoToNode : Node
    {
        public int Target;
        public GoToNode(Graph graph, NodeCanvas.DialogueTrees.GoToNode node, int ID) : base(graph, node, ID)
        {
            // connect target at the end
        }

        public override void Connect()
        {
            base.Connect();
            if (_graph.NodeIDs.TryGetValue(original, out var targetID))
                Target = targetID;
        }

        public GoToNode()
        {
            GraphDumper.DebugLog($"Creating empty GoToNode");
        }
    }

    [Serializable]
    public class Statement
    {
        public string Text;
        public string Audio;
        public string Meta;
        public Statement(NodeCanvas.DialogueTrees.Statement statement)
        {
            Text = statement.text;
            Audio = statement.audio.ToString();
            Meta = statement.meta;
        }

        public Statement()
        {
            GraphDumper.DebugLog($"Creating empty Statement");
        }
    }

    public class StatementNodeExt : Node
    {
        public Statement Statement;
        public StatementNodeExt(Graph graph, NodeCanvas.DialogueTrees.StatementNodeExt node, int ID) : base(graph, node, ID)
        {
            Statement = new Statement(node.statement);
        }

        public StatementNodeExt()
        {
            GraphDumper.DebugLog($"Creating empty StatementNodeExt");
        }
    }

    public class StatementNode : Node
    {
        public Statement Statement;
        public StatementNode(Graph graph, NodeCanvas.DialogueTrees.StatementNode node, int ID) : base(graph, node, ID)
        {
            Statement = new Statement(node.statement);
        }

        public StatementNode()
        {
            GraphDumper.DebugLog($"Creating empty StatementNode");
        }
    }

    [Serializable]
    public class Choice
    {
        public Statement Statement;
        public string Condition;
        public Choice(NodeCanvas.DialogueTrees.MultipleChoiceNode.Choice choice)
        {
            Statement = new Statement(choice.statement);
            Condition = "";
            if (choice.condition != null) 
            {
                Condition = choice.condition.summaryInfo;
            }
        }

        public Choice(NodeCanvas.DialogueTrees.MultipleChoiceNodeExt.Choice choice)
        {
            Statement = new Statement(choice.statement);
            Condition = "";
            if (choice.condition != null)
            {
                Condition = choice.condition.summaryInfo;
            }
        }

        public Choice()
        {
            GraphDumper.DebugLog($"Creating empty Choice");
        }
    }

    public class MultipleChoiceNode : Node
    {
        public float AvailableTime;
        public List<Choice> Choices;
        public MultipleChoiceNode(Graph graph, NodeCanvas.DialogueTrees.MultipleChoiceNode node, int ID) : base(graph, node, ID)
        {
            Choices = new();
            AvailableTime = node.availableTime;
            foreach (var choice in node.availableChoices)
            {
                Choices.Add(new Choice(choice));
            }
        }

        public MultipleChoiceNode()
        {
            GraphDumper.DebugLog($"Creating empty MultipleChoiceNode");
        }
    }

    public class MultipleChoiceNodeExt : Node
    {
        public float AvailableTime;
        public List<Choice> Choices;
        public MultipleChoiceNodeExt(Graph graph, NodeCanvas.DialogueTrees.MultipleChoiceNodeExt node, int ID) : base(graph, node, ID)
        {
            Choices = new();
            AvailableTime = node.availableTime;
            foreach (var choice in node.availableChoices)
            {
                Choices.Add(new Choice(choice));
            }
        }

        public MultipleChoiceNodeExt()
        {
            GraphDumper.DebugLog($"Creating empty MultipleChoiceNodeExt");
        }
    }
}
#pragma warning restore CS0618