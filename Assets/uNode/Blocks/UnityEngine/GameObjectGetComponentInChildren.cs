﻿using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "GameObject.GetComponentInChildren")]
	public class GameObjectGetComponentInChildren : Action {
		[ObjectType(typeof(GameObject))]
		public MemberData gameObject;
		[Filter(typeof(Component), OnlyGetType = true)]
		public MemberData componentType;
		[Filter(SetMember = true)]
		[ObjectType("componentType")]
		public MemberData storeComponent;

		protected override void OnExecute() {
			if(gameObject.isAssigned && componentType.isAssigned) {
				if(storeComponent.isAssigned) {
					storeComponent.Set(gameObject.Get<GameObject>().GetComponentInChildren(componentType.Get() as System.Type));
					return;
				}
				gameObject.Get<GameObject>().GetComponentInChildren(componentType.Get() as System.Type);
			}
		}

		public override string GenerateCode(Object obj) {
			if(gameObject.isAssigned && componentType.isAssigned) {
				string data = CG.Invoke(gameObject, "GetComponentInChildren", componentType.CGValue());
				if(string.IsNullOrEmpty(data)) return null;
				if(storeComponent.isAssigned) {
					return CG.Value((object)storeComponent) + " = " +
						data + " as " +
						CG.Type(componentType.type).AddSemicolon();
				}
				return data;
			}
			return null;
		}

		public override string GetDescription() {
			return "Returns the component of Type type in the GameObject or any of its children using depth first search.";
		}
	}
}