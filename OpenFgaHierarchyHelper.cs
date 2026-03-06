using System.Dynamic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ReqRespModels;
using UnitOfWorks;
using Db.Models;
using OpenFga.Sdk.Model;

namespace AccessAPIBase.Helpers
{
    /// <summary>
    /// OpenFga Hierarchy Helper
    /// </summary>
    public static class OpenFgaHierarchyHelper
    {
        /// <summary>
        /// Access Under Constant same as defined in openfga AuthorizationModel
        /// </summary>
        public static class AccessUnderConstant
        {
            public const string Country = "country";
            public const string State = "state";
            public const string City = "city";
            public const string StoreOutlet = "storeoutlet";
        }

        /// <summary>
        /// Hierarchy Node Keys
        /// </summary>
        public static class HierarchyNodeKeys
        {
            public static string Type = "Type";
            public static string Name = "Name";
            public static string Address = "Address";
            public static string Children = "Children";
        }

        /// <summary>
        /// Access For Contant same as defined in openfga AuthorizationModel
        /// </summary>
        public static class AccessForContant
        {
            public const string User = "user";
        }

        /// <summary>
        /// Access Type Contant same as defined in openfga AuthorizationModel
        /// </summary>
        public static class AccessTypeContant
        {
            public const string Writer = "writer";

            public const string Parent = "parent";
        }

        /// <summary>
        /// Get site hierarchy
        /// </summary>
        /// <param name="relations"></param>
        /// <param name="accessMap"></param>
        /// <returns></returns>
        public static List<HierarchyNode> BuildHierarchy(
        List<OpenFgaParentRelation> relations,
        Dictionary<string, OpenFgaAccessInfo> accessMap)
        {
            return BuildGenericHierarchy<HierarchyNode>(relations, accessMap, CreateHierarchyNode);
        }

        public static List<FullHierarchyNode> BuildFullHierarchy(
            List<OpenFgaParentRelation> relations,
            Dictionary<string, OpenFgaAccessInfo> accessMap)
        {
            return BuildGenericHierarchy<FullHierarchyNode>(relations, accessMap, CreateFullHierarchyNode);
        }

        /// <summary>
        /// Build Generic Hierarchy
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <param name="relations"></param>
        /// <param name="accessMap"></param>
        /// <param name="nodeFactory"></param>
        /// <returns></returns>
        private static List<TNode> BuildGenericHierarchy<TNode>(
            List<OpenFgaParentRelation> relations,
            Dictionary<string, OpenFgaAccessInfo> accessMap,
            Func<string, Dictionary<string, OpenFgaAccessInfo>, TNode> nodeFactory)
            where TNode : class, IHierarchyNode<TNode>, new()
        {
            var parentMap = relations
                .GroupBy(r => r.Parent)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Child).ToList());

            var allChildren = relations.Select(r => r.Child).ToHashSet();
            var allParents = relations.Select(r => r.Parent).ToHashSet();
            var roots = allParents.Except(allChildren).ToList();

            var result = new List<TNode>();

            foreach (var root in roots)
            {
                result.Add(BuildSubTree(root, parentMap, accessMap, nodeFactory));
            }

            return result;
        }

        /// <summary>
        /// Build Sub Tree for Children for parent
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <param name="objectId"></param>
        /// <param name="parentMap"></param>
        /// <param name="accessMap"></param>
        /// <param name="nodeFactory"></param>
        /// <returns></returns>
        private static TNode BuildSubTree<TNode>(
            string objectId,
            Dictionary<string, List<string>> parentMap,
            Dictionary<string, OpenFgaAccessInfo> accessMap,
            Func<string, Dictionary<string, OpenFgaAccessInfo>, TNode> nodeFactory)
            where TNode : class, IHierarchyNode<TNode>, new()
        {
            var node = nodeFactory(objectId, accessMap);

            if (parentMap.TryGetValue(objectId, out var children))
            {
                foreach (var child in children)
                {
                    node.Children.Add(BuildSubTree(child, parentMap, accessMap, nodeFactory));
                }
            }

            return node;
        }

        /// <summary>
        /// Create Hierarchy single Node
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="accessMap"></param>
        /// <returns></returns>
        private static HierarchyNode CreateHierarchyNode(string objectId, Dictionary<string, OpenFgaAccessInfo> accessMap)
        {
            var (type, name) = ParseObjectId(objectId);

            return new HierarchyNode
            {
                Type = type,
                Name = name,
            };
        }

        private static FullHierarchyNode CreateFullHierarchyNode(string objectId, Dictionary<string, OpenFgaAccessInfo> accessMap)
        {
            var (type, name) = ParseObjectId(objectId);

            return new FullHierarchyNode
            {
                Type = type,
                Name = name,
                Writers = accessMap.TryGetValue(objectId, out var access2) ? access2.Writers : new List<string>()
            };
        }

        /// <summary>
        /// Parse Object Id for differentiate values by :
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        private static (string Type, string Name) ParseObjectId(string objectId)
        {
            var parts = objectId.Split(':');
            return (parts[0], parts.Length > 1 ? parts[1] : null);
        }

        /// <summary>
        /// Get StoreOutlets Ids
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static List<string> GetStoreOutlets(List<HierarchyNode> nodes)
        {
            var storeOutlets = new List<string>();

            foreach (var node in nodes)
            {
                if (node.Type?.Equals(OpenFgaHierarchyHelper.AccessUnderConstant.StoreOutlet, StringComparison.OrdinalIgnoreCase) == true
                    && !string.IsNullOrEmpty(node.Name))
                {
                    storeOutlets.Add(node.Name);
                }

                if (node.Children != null && node.Children.Any())
                {
                    storeOutlets.AddRange(GetStoreOutlets(node.Children)); // Recursive call
                }
            }

            return storeOutlets;
        }

        /// <summary>
        /// SetAddress From Site
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="filteredSites"></param>
        /// <returns></returns>
        public static List<HierarchyNode> SetAddressFromSite(List<HierarchyNode> nodes, List<Site> filteredSites)
        {
            foreach (var node in nodes)
            {
                if (node.Type?.Equals(OpenFgaHierarchyHelper.AccessUnderConstant.StoreOutlet, StringComparison.OrdinalIgnoreCase) == true
                    && !string.IsNullOrEmpty(node.Name))
                {
                    var matchingDevice = filteredSites
                                            .FirstOrDefault(ds => ds.TdlinxStoreOutletCode == Convert.ToInt64(node.Name ?? "0"));

                    if (matchingDevice != null)
                    {
                        node.Address = matchingDevice.StoreOutletStreetAddress;
                    }
                }

                if (node.Children != null && node.Children.Any())
                {
                    SetAddressFromSite(node.Children, filteredSites); // Recursive
                }
            }
            return nodes;
        }

        /// <summary>
        /// Nodes Remove Address Except Storeoutlet
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static List<ExpandoObject> NodesRemoveAddressExceptStoreoutlet(List<HierarchyNode> nodes)
        {
            var result = new List<ExpandoObject>();

            foreach (var node in nodes)
            {
                dynamic expando = new ExpandoObject();
                var dict = (IDictionary<string, object?>)expando;

                dict[OpenFgaHierarchyHelper.HierarchyNodeKeys.Type] = node.Type;
                dict[OpenFgaHierarchyHelper.HierarchyNodeKeys.Name] = node.Name;

                if (node.Type.Equals(OpenFgaHierarchyHelper.AccessUnderConstant.StoreOutlet, StringComparison.OrdinalIgnoreCase) && node.Address != null)
                {
                    dict[OpenFgaHierarchyHelper.HierarchyNodeKeys.Address] = node.Address;
                }

                if (node.Children?.Any() == true)
                {
                    dict[OpenFgaHierarchyHelper.HierarchyNodeKeys.Children] = NodesRemoveAddressExceptStoreoutlet(node.Children);
                }
                else
                {
                    dict[OpenFgaHierarchyHelper.HierarchyNodeKeys.Children] = new List<ExpandoObject>();
                }

                result.Add(expando);
            }

            return result;
        }

        /// <summary>
        /// value to save in openfga to validate string due to whitespace in between not all in openFGA to save"
        /// </summary>
        /// <param name="input">input while saving to OpenFGA</param>
        /// <returns></returns>
        public static string OpenFGAFormatObjectToSave(string input)
        {
            return !string.IsNullOrEmpty(input) ? input.Replace(" ", "_") : input;
        }

        /// <summary>
        /// value to save in openfga to validate string due to whitespace in between not all in openFGA to save"
        /// </summary>
        /// <param name="input">input while getting from OpenFGA</param>
        /// <returns></returns>
        public static string OpenFGAFormatObjectToGet(string input)
        {
            return !string.IsNullOrEmpty(input) ? input.Trim().Replace("_", " ") : input;
        }

        public static FullHierarchyNode? TraverseAndFindMostParent(List<FullHierarchyNode> nodes, string userId, string objectType, string objectId, List<FullHierarchyNode> path)
        {
            foreach (var node in nodes)
            {
                var newPath = new List<FullHierarchyNode>(path) { node };

                if (node.Type.Equals(objectType, StringComparison.OrdinalIgnoreCase) &&
                    node.Name.Equals(objectId, StringComparison.OrdinalIgnoreCase))
                {
                    // Traverse the path upwards and find the most parent with writer access
                    foreach (var ancestor in newPath)
                    {
                        if (ancestor.Writers.Contains($"{OpenFgaHierarchyHelper.AccessForContant.User}:{userId}"))
                            return ancestor;
                    }
                    return null;
                }

                var found = TraverseAndFindMostParent(node.Children, userId, objectType, objectId, newPath);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
