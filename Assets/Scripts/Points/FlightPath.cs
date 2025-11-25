using System;
using System.Collections.Generic;
using UnityEngine;

namespace Points
{
	/// <summary>
	/// Represents test a flight path as an ordered list of point IDs with metadata.
	/// </summary>
	[System.Serializable]
	public class FlightPath
	{
		[SerializeField] private string _routeName;
		[SerializeField] private List<int> _pointIds = new List<int>();
		[SerializeField] private bool _isClosed;
		[SerializeField] private DateTime _createdAt;
		[SerializeField] private Color _pathColor;

		/// <summary>
		/// Human-readable name for this route (e.g., "Route A", "Main Path").
		/// </summary>
		public string RouteName
		{
			get => _routeName;
			set => _routeName = value;
		}

		/// <summary>
		/// Ordered list of point IDs that make up this path.
		/// </summary>
		public List<int> PointIds => _pointIds;

		/// <summary>
		/// Whether this path forms a closed loop (connects back to first point).
		/// </summary>
		public bool IsClosed
		{
			get => _isClosed;
			set => _isClosed = value;
		}

		/// <summary>
		/// When this route was created.
		/// </summary>
		public DateTime CreatedAt
		{
			get => _createdAt;
			set => _createdAt = value;
		}

		/// <summary>
		/// Color used for rendering this path's lines and indicators.
		/// </summary>
		public Color PathColor
		{
			get => _pathColor;
			set => _pathColor = value;
		}

		/// <summary>
		/// Number of points in this route.
		/// </summary>
		public int PointCount => _pointIds.Count;

		/// <summary>
		/// Whether this route has any points.
		/// </summary>
		public bool IsEmpty => _pointIds.Count == 0;

		/// <summary>
		/// Whether this route has enough points to form a valid path (2+ points).
		/// </summary>
		public bool IsValid => _pointIds.Count >= 2;

		/// <summary>
		/// Create a new flight path with default settings.
		/// </summary>
		public FlightPath(string routeName = null)
		{
			_routeName = routeName ?? $"Route {DateTime.Now:HHmmss}";
			_pointIds = new List<int>();
			_isClosed = false;
			_createdAt = DateTime.UtcNow;
			_pathColor = GetDefaultRouteColor(0);
		}

		/// <summary>
		/// Add a point ID to the end of this route.
		/// </summary>
		public void AddPoint(int pointId)
		{
			// Avoid duplicate consecutive points
			if (_pointIds.Count > 0 && _pointIds[_pointIds.Count - 1] == pointId)
			{
				return;
			}

			_pointIds.Add(pointId);
		}

		/// <summary>
		/// Remove the last point from this route.
		/// </summary>
		public bool RemoveLastPoint()
		{
			if (_pointIds.Count == 0) return false;
			_pointIds.RemoveAt(_pointIds.Count - 1);
			return true;
		}

		/// <summary>
		/// Thesis Feature: Remove a specific point from anywhere in the route.
		/// </summary>
		public bool RemovePoint(int pointId)
		{
			int index = _pointIds.IndexOf(pointId);
			if (index < 0) return false;
			
			_pointIds.RemoveAt(index);
			
		// Insert an explicit gap marker (0) only when removing from the middle
		// This prevents auto-connecting neighbors in rendering
		bool removedFromMiddle = index > 0 && index < _pointIds.Count;
		if (removedFromMiddle)
		{
			int prevIdx = index - 1;
			int nextIdx = index; // after removal, next element is now at 'index'
			// Check if neighbors are breaks (0 only, NOT Start/End which are -1/-2)
			bool prevIsBreak = _pointIds[prevIdx] == 0;
			bool nextIsBreak = _pointIds[nextIdx] == 0;
			if (!prevIsBreak && !nextIsBreak)
			{
				_pointIds.Insert(index, 0);
			}
		}
			return true;
		}

		/// <summary>
		/// Remove the first break marker (<= 0) between two existing points to merge segments.
		/// Returns true if a break was removed.
		/// </summary>
		public bool RemoveFirstBreakBetween(int fromPointId, int toPointId)
		{
			int fromIndex = _pointIds.IndexOf(fromPointId);
			int toIndex = _pointIds.IndexOf(toPointId);
			if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex)
			{
				return false;
			}

			if (fromIndex > toIndex)
			{
				// Ensure fromIndex < toIndex for forward scan
				int tmp = fromIndex;
				fromIndex = toIndex;
				toIndex = tmp;
			}

			for (int i = fromIndex + 1; i < toIndex; i++)
			{
				if (_pointIds[i] <= 0)
				{
					_pointIds.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Insert a new point immediately after the given anchor point in the route.
		/// If the point already exists in the route, returns false.
		/// </summary>
		public bool InsertPointAfter(int anchorPointId, int newPointId)
		{
			if (newPointId <= 0) return false; // don't insert break markers as points
			if (_pointIds.Contains(newPointId)) return false;

			int anchorIndex = _pointIds.IndexOf(anchorPointId);
			if (anchorIndex < 0)
			{
				return false;
			}

			int insertIndex = Mathf.Min(anchorIndex + 1, _pointIds.Count);
			_pointIds.Insert(insertIndex, newPointId);
			return true;
		}

		/// <summary>
		/// Clear all points from this route.
		/// </summary>
		public void Clear()
		{
			_pointIds.Clear();
			_isClosed = false;
		}

		/// <summary>
		/// Get the world positions for all points in this route.
		/// </summary>
		public List<Vector3> GetWorldPositions(PointPlacementManager manager)
		{
			var positions = new List<Vector3>();
			foreach (int pointId in _pointIds)
			{
				var pointHandle = manager.GetPoint(pointId);
				if (pointHandle != null)
				{
					positions.Add(pointHandle.transform.position);
				}
			}
			return positions;
		}

		/// <summary>
		/// Check if this route contains a specific point ID.
		/// </summary>
		public bool ContainsPoint(int pointId)
		{
			return _pointIds.Contains(pointId);
		}

		/// <summary>
		/// Get the index of a point in this route, or -1 if not found.
		/// </summary>
		public int GetPointIndex(int pointId)
		{
			return _pointIds.IndexOf(pointId);
		}

		/// <summary>
		/// Create a resampled version of this path with evenly spaced points.
		/// </summary>
		public List<Vector3> GetResampledPath(PointPlacementManager manager, float spacing = 0.5f)
		{
			var positions = GetWorldPositions(manager);
			if (positions.Count < 2) return positions;

			var resampled = new List<Vector3>();
			resampled.Add(positions[0]); // Always include first point

			for (int i = 0; i < positions.Count - 1; i++)
			{
				Vector3 start = positions[i];
				Vector3 end = positions[i + 1];
				Vector3 direction = (end - start).normalized;
				float distance = Vector3.Distance(start, end);

				float currentDistance = spacing;
				while (currentDistance < distance)
				{
					Vector3 interpolatedPoint = start + direction * currentDistance;
					resampled.Add(interpolatedPoint);
					currentDistance += spacing;
				}
			}

			// Always include last point
			resampled.Add(positions[positions.Count - 1]);

			// If closed, add points from last to first
			if (_isClosed && positions.Count > 2)
			{
				Vector3 lastToFirst = positions[0] - positions[positions.Count - 1];
				float lastDistance = lastToFirst.magnitude;
				Vector3 lastDirection = lastToFirst.normalized;

				float currentDistance = spacing;
				while (currentDistance < lastDistance)
				{
					Vector3 interpolatedPoint = positions[positions.Count - 1] + lastDirection * currentDistance;
					resampled.Add(interpolatedPoint);
					currentDistance += spacing;
				}
			}

			return resampled;
		}

		/// <summary>
		/// Get a default color for a route based on its index.
		/// </summary>
	private static Color GetDefaultRouteColor(int routeIndex)
	{
		// Use darker cyan for all routes - easier on the eyes
		return new Color(0f, 0.7f, 0.7f); // Darker cyan (was 0, 1, 1)
	}
	}
}
