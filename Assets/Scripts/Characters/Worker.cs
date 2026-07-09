using UnityEngine;
using MiniMart.Core;
using MiniMart.AI;
using MiniMart.Map;
using System.Collections.Generic;

namespace MiniMart.Characters
{
    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))] // Or your pathfinding component
    public class Worker : MonoBehaviour
    {
        public RoleType Role;
        
        private WorkerTask currentTask;
        private GridPathfinder pathfinder;
        private PlayerController playerRef; // Reference to player for distance checks if needed

        public void Configure(RoleType role)
        {
            Role = role;
            // Initialize colors, speed, capacity based on role
        }

        private void Start()
        {
            pathfinder = GridPathfinder.Instance;
        }

        private void Update()
        {
            if (currentTask == null)
            {
                currentTask = TaskSystem.Instance?.GetNextTask(Role);
            }
            else
            {
                ExecuteTask();
            }
        }

        private void ExecuteTask()
        {
            // Move to currentTask.TargetLocation
            // Perform Action (Harvest, Stock, Process, Cashier)
            // Once complete:
            // TaskSystem.Instance.ReportTaskComplete(currentTask);
            // currentTask = null;
        }
    }
}
