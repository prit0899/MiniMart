using System.Collections.Generic;
using UnityEngine;
using MiniMart.Core;
using MiniMart.Economy;
using MiniMart.Production;

namespace MiniMart.AI
{
    public enum TaskAction
    {
        Harvest,
        StockShelf,
        Process,
        Cashier
    }

    public class WorkerTask
    {
        public TaskAction Action;
        public Vector3 TargetLocation;
        public ItemType Item;
        public int Amount;
        public object TargetContext; // e.g., the Farm, Machine, or Shelf component
    }

    public class TaskSystem : MonoBehaviour
    {
        public static TaskSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public WorkerTask GetNextTask(RoleType role)
        {
            // Placeholder: currently returns null to prevent NullReferenceExceptions
            // Full logic will be wired up during Phase 4 when Worker.cs is integrated.
            return null;
        }

        public void ReportTaskComplete(WorkerTask task)
        {
            // Worker calls this when finished.
        }
    }
}
