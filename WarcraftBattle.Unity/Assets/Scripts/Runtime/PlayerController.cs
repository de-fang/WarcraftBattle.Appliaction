using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using WarcraftBattle3D.Core;

namespace WarcraftBattle3D
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private GameRuntimeHost runtimeHost;
        [SerializeField]
        private LayerMask groundLayer;

        private readonly List<Entity> _selectedEntities = new List<Entity>();
        private Camera _mainCamera;

        private void Start()
        {
            if (runtimeHost == null)
            {
                runtimeHost = FindAnyObjectByType<GameRuntimeHost>();
            }
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (runtimeHost == null || runtimeHost.Engine == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0)) // Left Click - Select
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
                SelectEntity();
            }

            if (Input.GetMouseButtonDown(1)) // Right Click - Move/Attack
            {
                IssueCommand();
            }
        }

        private void SelectEntity()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                 // Check if we hit an entity visualization
                 // Currently visualizations don't have colliders (EntityPrimitiveView removes them).
                 // We might need to rely on proximity or add colliders for selection.

                 // However, the ground plane should have a collider (if we create one).
                 // For now let's assume we search for closest entity to hit point on ground plane.

                 // If we hit ground (layer check needed ideally)
                 Vector3 point = hit.point;
                 double x = point.x / 0.01f; // Reversing scale from EntityPrimitiveView
                 double y = point.z / 0.01f; // Z is Y in 2D engine

                 // Wait, EntityPrimitiveView uses `worldScale` which is dynamic.
                 // We need to access that scale or use a consistent conversion.
                 // EntityPrimitiveView: `float x = (float)entity.X * worldScale;`
                 // worldScale defaults to 0.01 but is calculated from TileSize.

                 // Let's assume EntityPrimitiveView has a static way to access scale or we query positions directly.
                 // Or we can iterate all entities and check screen space distance.

                 Entity best = null;
                 float minScreenDist = 50f; // px

                 foreach(var entity in runtimeHost.Engine.Entities)
                 {
                     if (!entity.IsAlive) continue;
                     // Convert entity pos to screen
                     // Need the actual visual position.
                     // The visual position is calculated in EntityPrimitiveView.
                     // Let's try to do it via Raycast if we add colliders back?
                     // Or just do a World To Screen check.

                     // We need the world scale to know where it is in 3D.
                     // Let's find EntityPrimitiveView instance.
                 }
            }

            // Simpler approach for now:
            // Since I don't want to modify EntityPrimitiveView too much yet to expose scale,
            // I will implement a simpler Raycast based selection by ADDING colliders to unit views in ModelFactory/EntityPrimitiveView.
            // But EntityPrimitiveView explicitly calls RemoveCollider.

            // I will modify EntityPrimitiveView to keep colliders but put them on a specific layer if possible, or just keep them.
        }

        private void IssueCommand()
        {
            // Similar issue, need to know where we clicked in game coordinates.
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            // We need a ground plane.
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.GetPoint(enter);

                // Convert back to game coordinates.
                // We need the scale factor.
                // Let's assume we can get it from EntityPrimitiveView or recalculate it.
                // scale = unityTileSize / engine.TileSize;

                var view = FindAnyObjectByType<EntityPrimitiveView>();
                float scale = view != null ? view.WorldScale : 0.01f;

                double gx = point.x / scale;
                double gy = point.z / scale;

                foreach(var entity in _selectedEntities)
                {
                    if (entity is Unit unit)
                    {
                        unit.OrderMove(gx, gy);
                    }
                }
            }
        }
    }
}
