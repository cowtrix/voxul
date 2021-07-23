using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Voxul.Utilities
{
	public class VoxelTree<T> where T: struct
	{
		public void DrawGizmos()
		{
			void giz(Node n)
			{
				foreach(var c in n.Children)
				{
					giz(c.Value);
				}
				if(n is LeafNode)
				{
					Gizmos.color = Color.white.WithAlpha(.5f);
				}
				else if (n is Partition)
				{
					Gizmos.color = Color.green.WithAlpha(.5f);
				}
				Gizmos.DrawWireCube(n.Coordinate.ToVector3(), Vector3.one * n.Coordinate.GetScale());
			}	
			giz(m_root);
		}

		public abstract class Node 
		{
			public VoxelCoordinate Coordinate { get; }

			public Dictionary<VoxelCoordinate, Node> Children = new Dictionary<VoxelCoordinate, Node>();

			public Node(VoxelCoordinate coordinate)
			{
				Coordinate = coordinate;
			}

			internal IEnumerable<(VoxelCoordinate, T)> GetAllDescendants()
			{
				foreach(var childNode in Children)
				{
					if(childNode.Value is LeafNode leaf)
					{
						yield return (leaf.Coordinate, leaf.Value);
					}
					else if (childNode.Value is Partition partition)
					{
						foreach(var descendant in partition.GetAllDescendants())
						{
							yield return descendant;
						}
					}
				}
			}
		}

		public class LeafNode : Node
		{
			public T Value;

			public LeafNode(VoxelCoordinate coordinate, T value) : base(coordinate)
			{
				Value = value;
			}
		}

		public class Partition : Node
		{
			public Partition(VoxelCoordinate coordinate) : base(coordinate)
			{
			}
		}
		public sbyte MinLayer { get; }
		private Partition m_root { get; }

		public VoxelTree(sbyte maxLayer)
		{
			MinLayer = maxLayer;
			m_root = new Partition(new VoxelCoordinate { Layer = MinLayer });
		}

		public VoxelTree(sbyte maxLayer, IDictionary<VoxelCoordinate, T> data) : this(maxLayer)
		{
			foreach(var d in data)
			{
				Insert(d.Key, d.Value);
			}
		}

		public bool TryGetValue(VoxelCoordinate coord, out T value)
		{
			if(TryGetValue(coord, out Node node) && node is LeafNode n)
			{
				value = n.Value;
				return true;
			}
			value = default;
			return false;
		}

		public bool TryGetValue(VoxelCoordinate coord, out Node value)
		{
			if(coord.Layer < MinLayer)
			{
				value = null;
				return false;
			}
			sbyte currentLayer = MinLayer;
			Node lastNode = m_root;
			bool doneRoot = false;
			while (true)
			{
				if (doneRoot)
				{
					// Move a layer closer
					currentLayer = (sbyte)Mathf.MoveTowards(currentLayer, coord.Layer, 1);
				}
				doneRoot = true;

				var closerCoord = coord.ChangeLayer(currentLayer);
				if (closerCoord == coord)
				{
					// We're there
					return lastNode.Children.TryGetValue(closerCoord, out value);
				}

				if (!lastNode.Children.TryGetValue(closerCoord, out var closerNode)
					|| closerNode is LeafNode)
				{
					closerNode = new Partition(closerCoord);
					lastNode.Children[closerCoord] = closerNode;
				}
				lastNode = closerNode;
			}
		}

		public void Insert(VoxelCoordinate coord, T value)
		{
			if (coord.Layer < MinLayer)
			{
				foreach(var sub in coord.Subdivide())
				{
					Insert(sub, value);
				}
				return;
			}

			sbyte currentLayer = MinLayer;
			var startingCoord = coord.ChangeLayer(0);
			if (coord == startingCoord)
			{
				m_root.Children[coord] = new LeafNode(coord, value);
				return;
			}

			Node lastNode = m_root;
			bool doneRoot = false;
			while (true)
			{
				if (doneRoot)
				{
					// Move a layer closer
					currentLayer = (sbyte)Mathf.MoveTowards(currentLayer, coord.Layer, 1);
				}
				doneRoot = true;

				var closerCoord = coord.ChangeLayer(currentLayer);
				if (closerCoord == coord)
				{
					// We're there
					lastNode.Children[closerCoord] = new LeafNode(closerCoord, value);
					return;
				}

				if (!lastNode.Children.TryGetValue(closerCoord, out var closerNode)
					|| closerNode is LeafNode)
				{
					closerNode = new Partition(closerCoord);
					lastNode.Children[closerCoord] = closerNode;
				}
				lastNode = closerNode;
			}
		}
	}
}
