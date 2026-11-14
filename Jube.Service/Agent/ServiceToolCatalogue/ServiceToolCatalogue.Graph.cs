/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.Service.Agent.Models;
using Jube.Service.Query.Graph;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddGraph(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "GraphNeighbourhoodGet", OperationKind.Read, true, false,
                    "Returns the network around a key value under a Graph Registry: nodes within the requested hops, " +
                    "the edges among them with the archive ids behind each, hubs hidden unless asked, clusters " +
                    "collapsed beyond the cap.",
                    GraphService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphExplodeGet", OperationKind.Read, true, false,
                    "Lists the one-hop neighbourhood of a node as lanes with counts, totals, first and last seen, " +
                    "hub flag and cases.",
                    GraphService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphEdgeTransactionsGet", OperationKind.Read, true, false,
                    "Lists the archived transactions behind one edge within a window, with archive ids for the " +
                    "Journal filter.",
                    GraphService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphNodeGet", OperationKind.Read, true, false,
                    "Returns one node's cached graph features, edge definitions with counts, cases and plain " +
                    "sentences stating why it matters.",
                    GraphService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphRenderSvg", OperationKind.Read, true, false,
                    "Draws the network around a key value as an SVG with the requested layout and returns the node " +
                    "positions for the next render.",
                    GraphService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphTimeStripSvg", OperationKind.Read, true, false,
                    "Draws the histogram of the transactions behind a neighbourhood's edges within the window.",
                    GraphService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphCaptureCreate", OperationKind.Write, false, false,
                    "Freezes the current network view into a case as two hashed case files under one note; calling " +
                    "twice stores two captures.",
                    GraphService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphPathsGet", OperationKind.Read, true, false,
                    "Finds up to K shortest paths between two nodes with the nodes and edges touched and a sentence " +
                    "per path.",
                    GraphService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphFlowGet", OperationKind.Read, true, false,
                    "Follows the money forward from or back to a node through time-ordered, amount-matched hops and " +
                    "returns the tranches, pass-through nodes and archive ids.",
                    GraphService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphPatternList", OperationKind.Read, true, false,
                    "Lists detected money-flow patterns (fan-in, fan-out, cycles, pass-through, smurfing, structuring, " +
                    "dense blocks) with anchor, score, window and evidence.",
                    GraphAnalyticsService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphPatternGet", OperationKind.Read, true, false,
                    "Returns one detected pattern with its members, roles and archive ids.",
                    GraphAnalyticsService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphPatternDecide", OperationKind.Write, true, false,
                    "Marks a detected pattern Seen, Dismissed or Cased with a case id.",
                    GraphAnalyticsService.ResolvePermissions),
                new ServiceToolDescriptor(
                    "GraphCommunityList", OperationKind.Read, true, false,
                    "Lists the graph communities with size, density, confirmed and closed counts and the ratio to the " +
                    "tenant rate.",
                    GraphAnalyticsService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphCommunityGet", OperationKind.Read, true, false,
                    "Returns one community with its most central members.",
                    GraphAnalyticsService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphHubList", OperationKind.Read, true, false,
                    "Lists the nodes marked as hubs with degrees and overrides.",
                    GraphAnalyticsService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphHubOverride", OperationKind.Write, true, false,
                    "Overrides whether a node is a hub: never, always, or back to the cycle's decision.",
                    GraphAnalyticsService.ResolvePermissions),
                new ServiceToolDescriptor(
                    "GraphExplorationList", OperationKind.Read, true, false,
                    "Lists the caller's saved Graph page explorations, newest first.",
                    GraphService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphExplorationSave", OperationKind.Write, true, false,
                    "Saves or overwrites one of the caller's Graph page explorations by name.",
                    GraphService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "GraphExplorationDelete", OperationKind.Delete, true, true,
                    "Removes one of the caller's saved explorations; reversible only by direct database action.",
                    GraphService.RequiredPermissions)
            ]);
        }
    }
}
