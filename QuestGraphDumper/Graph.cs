using NodeCanvas.Framework;
using NodeCanvas.StateMachines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Networking.Types;
using static Unity.Audio.Handle;

namespace QuestGraphDumper
{
    public class Graph
    {
        public List<Node> Nodes;
        [XmlIgnore]
        Dictionary<NodeCanvas.Framework.Node, int> NodeIDs;
        public Graph(QuestTree tree) 
        {
            QuestGraphDumper.DebugLog($"Creating graph from {tree}");
            Nodes = new();
            NodeIDs = new();
            var i = 0;
            foreach (var node in tree.allNodes)
            {
                QuestGraphDumper.DebugLog($"{i}: {node}");
                Node newNode = null;
                if (node is NodeCanvas.StateMachines.ActionState actionState)
                    newNode = new ActionState(actionState, i);
                else if (node is NodeCanvas.StateMachines.ConcurrentState concurState)
                    newNode = new ConcurrentState(concurState, i);
                else if (node is NodeCanvas.StateMachines.AnyState anyState)
                    newNode = new AnyState(anyState, i);

                QuestGraphDumper.DebugLog($"Created node {newNode}");
                Nodes.Add(newNode);
                NodeIDs.Add(node, i);
                i++;
            }

            //TODO: insert right ids into connections
            foreach (var node in Nodes)
            {
                node.Connect(NodeIDs);
            }
        }

        public Graph()
        {
            QuestGraphDumper.DebugLog($"Creating empty graph");
            Nodes = new();
            NodeIDs = new();
        }
    }

    public class Connection
    {
        public string Condition;
        public int TargetID;

        public Connection(string condition, int target)
        {
            Condition = condition;
            TargetID = target;
        }

        public Connection()
        {
            QuestGraphDumper.DebugLog($"Creating empty connection");
        }
    }

    //INFO: the xmlserializer needs to know all subtypes so we just xmlinclude them here
    [XmlInclude(typeof(ActionState))]
    [XmlInclude(typeof(ConcurrentState))]
    [XmlInclude(typeof(AnyState))]
    public class Node
    {
        public int ID;
        public string Name;
        public List<Connection> Outgoing;
        private NodeCanvas.Framework.Node original;

        public Node(NodeCanvas.StateMachines.FSMState node, int id)
        {
            Outgoing = new();
            ID = id;
            Name = node.name;

            original = node;
        }

        public Node()
        {
            QuestGraphDumper.DebugLog($"Creating empty node");
        }

        public void Connect(Dictionary<NodeCanvas.Framework.Node, int> NodeIDs)
        {
            foreach (var outgoing in original.outConnections)
            {
                if (outgoing is not FSMConnection fsmConnection)
                    throw new Exception("Connection is not a FSMConnection");

                if (!NodeIDs.TryGetValue(outgoing.targetNode, out var targetID))
                    throw new Exception("TargetNode has no ID");

                //TODO: better field than info? summaryInfo?
                var text = "";
                if (fsmConnection.condition != null)
                    text = Sanitize(fsmConnection.condition.summaryInfo);
                Outgoing.Add(new Connection(text, targetID));
            }
        }

        //TODO: transform to patches?
        public string TaskString(NodeCanvas.Framework.Task task)
        {
            if (task is ActionTask)
            {
                return (task.agentIsOverride ? "* " : "") + task.info;
            }
            if (task is ConditionTask conditionTask)
            {
                return (task.agentIsOverride ? "* " : "") + (conditionTask.invert ? "If ! " : "If ") + task.info;
            }
            return task.info;
        }
        public string ActionListString(NodeCanvas.Framework.ActionList actionList)
        {
            string text = (actionList.actions.Count > 1) ?
                    (actionList.executionMode == NodeCanvas.Framework.ActionList.ActionsExecutionMode.ActionsRunInSequence
                        ? "In Sequence" : "In Parallel")
                    : string.Empty;
            for (int i = 0; i < actionList.actions.Count; i++)
            {
                var task = actionList.actions[i];
                text += $"- {TaskString(task)}{((i == actionList.actions.Count - 1) ? "" : "\n")}";
            }
            return text;
        }
        public string ConditionListString(NodeCanvas.Framework.ConditionList conditionList)
        {
            string text = (conditionList.conditions.Count > 1) ? ("(" + (conditionList.allTrueRequired ? "ALL True" : "ANY True") + ")\n") : string.Empty;
            for (int i = 0; i < conditionList.conditions.Count; i++)
            {
                if (conditionList.conditions[i] != null && (conditionList.conditions[i].isActive || (conditionList.initialActiveConditions != null && conditionList.initialActiveConditions.Contains(conditionList.conditions[i]))))
                {
                    string str = "- ";
                    text = text + str + TaskString(conditionList.conditions[i]) + ((i == conditionList.conditions.Count - 1) ? "" : "\n");
                }
            }
            return text;
        }

        public string Sanitize(string text)
        {
            //TODO: remove html tags?
            return text;
        }
    }

    [Serializable]
    public class ActionState : Node
    {
        //TODO: as list?
        public string ActionList;
        public ActionState(NodeCanvas.StateMachines.ActionState node, int ID) : base(node, ID)
        {
            ActionList = "";
            if (node.actionList != null)
                ActionList = Sanitize(node.actionList.info);
        }

        public ActionState()
        {
            QuestGraphDumper.DebugLog($"Creating empty ActionState");
        }
    }



    [Serializable]
    public class ConcurrentState : Node
    {
        //TODO: do as lists?
        public string ConditionList;
        public string ActionList;
        public ConcurrentState(NodeCanvas.StateMachines.ConcurrentState node, int ID) : base(node, ID)
        {
            //TODO: better stringify than this?
            ConditionList = "";
            if (node.conditionList != null)
            {
                ConditionList = Sanitize(node.conditionList.info);
            }
            ActionList = "";
            if (node.actionList != null)
            {
                ActionList = Sanitize(node.actionList.info);
            }
        }

        public ConcurrentState()
        {
            QuestGraphDumper.DebugLog($"Creating empty ConcurrentState");
        }
    }

    [Serializable]
    public class AnyState : Node
    {
        public AnyState(NodeCanvas.StateMachines.AnyState node, int ID) : base(node, ID)
        {
            //nothing here yet
        }

        public AnyState()
        {
            QuestGraphDumper.DebugLog($"Creating empty AnyState");
        }
    }
}
