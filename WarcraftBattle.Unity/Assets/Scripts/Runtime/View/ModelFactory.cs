using System.Collections.Generic;
using UnityEngine;
using WarcraftBattle3D.Core;

namespace WarcraftBattle3D
{
    public class ModelFactory : MonoBehaviour
    {
        public static ModelFactory Instance { get; private set; }

        [SerializeField]
        private Material humanMaterial;
        [SerializeField]
        private Material orcMaterial;
        [SerializeField]
        private Material neutralMaterial;

        private void Awake()
        {
            Instance = this;
        }

        public GameObject CreateModel(Entity entity, Transform parent)
        {
            if (entity is Unit unit)
            {
                return CreateUnitModel(unit, parent);
            }
            if (entity is Building building)
            {
                return CreateBuildingModel(building, parent);
            }
            if (entity is Obstacle obstacle)
            {
                return CreateObstacleModel(obstacle, parent);
            }

            return CreateDefaultModel(entity, parent);
        }

        private GameObject CreateUnitModel(Unit unit, Transform parent)
        {
            var go = new GameObject(unit.Key);
            go.transform.SetParent(parent, false);

            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, 1, 0);
            body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            ApplyTeamMaterial(unit.Team, body);
            Destroy(body.GetComponent<Collider>());

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0, 2.2f, 0);
            head.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            ApplyTeamMaterial(unit.Team, head);
            Destroy(head.GetComponent<Collider>());

            // Weapon (Right Hand)
            var weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.transform.SetParent(go.transform, false);
            weapon.transform.localPosition = new Vector3(0.6f, 1.2f, 0.5f);
            weapon.transform.localScale = new Vector3(0.2f, 0.2f, 1.0f);
            ApplyTeamMaterial(unit.Team, weapon); // Maybe different color?
            Destroy(weapon.GetComponent<Collider>());

            // If ranged, maybe a bow-like structure?
            if (unit.Stats.Type == UnitType.Ranged)
            {
                 weapon.transform.localScale = new Vector3(0.2f, 1.2f, 0.2f);
                 weapon.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }

            return go;
        }

        private GameObject CreateBuildingModel(Building building, Transform parent)
        {
            var go = new GameObject(building.Info.Id);
            go.transform.SetParent(parent, false);

            // Main Structure
            var main = GameObject.CreatePrimitive(PrimitiveType.Cube);
            main.transform.SetParent(go.transform, false);
            main.transform.localPosition = new Vector3(0, 1, 0);
            // Scale will be handled by EntityPrimitiveView based on entity size
            ApplyTeamMaterial(building.Team, main);
            Destroy(main.GetComponent<Collider>());

            // Roof
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roof.transform.SetParent(go.transform, false);
            roof.transform.localPosition = new Vector3(0, 2f, 0);
            roof.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
            ApplyTeamMaterial(building.Team, roof);
            Destroy(roof.GetComponent<Collider>());

            return go;
        }

        private GameObject CreateObstacleModel(Obstacle obstacle, Transform parent)
        {
             var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
             go.transform.SetParent(parent, false);
             go.name = obstacle.Type;
             var renderer = go.GetComponent<Renderer>();
             if (renderer != null)
             {
                 renderer.material.color = Color.gray;
             }
             Destroy(go.GetComponent<Collider>());
             return go;
        }

        private GameObject CreateDefaultModel(Entity entity, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(parent, false);
            ApplyTeamMaterial(entity.Team, go);
            Destroy(go.GetComponent<Collider>());
            return go;
        }

        private void ApplyTeamMaterial(TeamType team, GameObject go)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            if (team == TeamType.Human && humanMaterial != null)
            {
                renderer.sharedMaterial = humanMaterial;
            }
            else if (team == TeamType.Orc && orcMaterial != null)
            {
                renderer.sharedMaterial = orcMaterial;
            }
            else if (neutralMaterial != null)
            {
                renderer.sharedMaterial = neutralMaterial;
            }
            else
            {
                // Fallback color
                if (team == TeamType.Human) renderer.material.color = Color.blue;
                else if (team == TeamType.Orc) renderer.material.color = Color.red;
                else renderer.material.color = Color.white;
            }
        }
    }
}
