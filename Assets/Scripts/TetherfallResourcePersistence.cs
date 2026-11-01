using System.Collections.Generic;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private readonly List<ResourceNode> resourceNodes = new List<ResourceNode>();

        public void RegisterResourceNode(ResourceNode node)
        {
            if (node != null && !resourceNodes.Contains(node))
            {
                resourceNodes.Add(node);
                RegisterResourceNodeFeedback(node);
            }
        }

        public void OnResourceNodeStateChanged()
        {
            PlayResourceNodeStateFeedback();
            RequestSave();
        }

        private void WriteResourceNodeSave(TetherfallSaveData data)
        {
            data.resourceNodes.Clear();
            foreach (var node in resourceNodes)
            {
                if (node != null && !node.IsAvailable)
                {
                    data.resourceNodes.Add(node.WriteSaveData());
                }
            }
        }

        private void RestoreResourceNodeState(TetherfallSaveData data)
        {
            var savedNodes = new Dictionary<string, ResourceNodeSaveData>();
            foreach (var savedNode in data.resourceNodes)
            {
                if (savedNode != null && !string.IsNullOrEmpty(savedNode.id))
                {
                    savedNodes[savedNode.id] = savedNode;
                }
            }

            foreach (var node in resourceNodes)
            {
                if (node != null && savedNodes.TryGetValue(node.PersistenceId, out var savedNode))
                {
                    node.RestoreSaveData(savedNode);
                }
            }

            SynchronizeResourceNodeFeedbackState();
        }
    }
}
