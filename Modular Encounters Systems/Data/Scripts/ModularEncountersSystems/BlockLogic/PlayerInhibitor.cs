﻿using ModularEncountersSystems.Entities;
using ModularEncountersSystems.Tasks;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ModularEncountersSystems.BlockLogic {
	public class PlayerInhibitor : InhibitorLogic, IBlockLogic {

		internal List<PlayerEntity> _playersInBlockRange;

		internal float _damageAtZeroDistance = 10;

		public PlayerInhibitor(BlockEntity block) {

			Setup(block);

		}

		internal override void Setup(BlockEntity block) {

			_fixCheck = true;
			base.Setup(block);

			if (!_isServer) {

				_isValid = false;
				return;

			}

			_playersInBlockRange = new List<PlayerEntity>();

			if (_antenna != null) {

				_antenna.Radius = 800;
				_antenna.CustomName = "[Personnel Inhibitor Field]";
				_antenna.CustomNameChanged += NameChange;

			} else {

				_antennaRange = 800;

			}

			_logicType = "Player Inhibitor";
			_inhibitor = InhibitorTypes.Personnel;
			_useTick60 = true;
			_useTick100 = true;

		}

		internal void NameChange(IMyTerminalBlock block) {
		
			if(_antenna.CustomName != "[Personnel Inhibitor Field]")
				_antenna.CustomName = "[Personnel Inhibitor Field]";

		}

		internal override void RunTick60() {

			if (!_isWorking || !Active || !_playersInRange)
				return;

			foreach (var player in _playersInBlockRange) {

				if (!player.IsPlayerStandingCharacter())
					continue;

				float distanceRatio = 1 - (float)(Vector3D.Distance(player.GetPosition(), Entity.GetPosition()) / _antennaRange);

				player.Player.Character.DoDamage(_damageAtZeroDistance * distanceRatio, MyStringHash.GetOrCompute("Radiation"), true, null, Entity.EntityId);

			}

		}

		internal override void RunTick100() {

			if (!_isWorking || !Active)
				return;

			if (_antenna != null && _antenna.Radius != _antennaRange) {

				_antennaRange = _antenna.Radius;

			}

			//Check Player Distances and Status
			foreach (var player in PlayerManager.Players) {

				if (!player.ActiveEntity() || (player.PlayerInhibitorNullifier != null && player.PlayerInhibitorNullifier.EffectActive())) {

					RemovePlayer(player);
					continue;

				}

				var characterPos = player.GetCharacterPosition();

				if (!player.IsPlayerStandingCharacter()) {

					RemovePlayer(player);
					continue;

				} else if(characterPos == Vector3D.Zero) {

					characterPos = player.GetPosition();
				
				}

				var distance = Vector3D.Distance(Entity.GetPosition(), characterPos);

				if (distance > _antennaRange) {

					RemovePlayer(player);
					continue;

				}

				if (distance <= _antennaRange) {

					if (!ProcessInhibitorSuitUpgrades(player)) {

						if (!_playersInBlockRange.Contains(player)) {

							MyVisualScriptLogicProvider.ShowNotification("WARNING: Prolonged Exposure To Personnel Inhibitor Field May Be Fatal!", 5000, "Red", player.Player.IdentityId);
							_playersInBlockRange.Add(player);
							player.AddInhibitorToPlayer(_antenna, _inhibitor);

						}

					}

				}
			
			}

			_playersInRange = _playersInBlockRange.Count > 0;

		}

		internal void RemovePlayer(PlayerEntity player) {

			_playersInBlockRange.Remove(player);
			player.RemoveInhibitorFromPlayer(_antenna, _inhibitor);

		}

		internal override void Unload(IMyEntity entity = null) {

			base.Unload(entity);

			if (_antenna != null) {

				_antenna.CustomNameChanged -= NameChange;

			}
		
		}

	}

}
