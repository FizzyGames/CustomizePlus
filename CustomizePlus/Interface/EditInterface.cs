﻿// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Numerics;
	using System.Windows.Forms;
	using Anamnesis.Files;
	using Anamnesis.Posing;
	using CustomizePlus.Helpers;
	using CustomizePlus.Memory;
	using Dalamud.Interface;
	using Dalamud.Interface.Components;
	using Dalamud.Logging;
	using ImGuiNET;
	using Newtonsoft.Json;
	using static CustomizePlus.BodyScale;

	public class EditInterface : WindowBase
	{
		protected BodyScale? Scale { get; private set; }

		protected override string Title => $"(WIP) Edit Scale: {this.originalScaleName}";
		protected BodyScale? ScaleUpdated { get; private set; }

		private int scaleIndex = -1;

		private string newScaleName = string.Empty;
		private string newScaleCharacter = string.Empty;
		private string originalScaleName = string.Empty;
		private string originalScaleCharacter = string.Empty;
		private BoneEditsContainer rootEditsContainer = new BoneEditsContainer();

		private Dictionary<string, BoneEditsContainer> boneValuesOriginal = new Dictionary<string, BoneEditsContainer>();
		private Dictionary<string, BoneEditsContainer> boneValuesNew = new Dictionary<string, BoneEditsContainer>();
		private readonly List<string> boneNamesLegacy = LegacyBoneNameConverter.GetLegacyNames();
		private readonly List<string> boneNamesModern = LegacyBoneNameConverter.GetModernNames();
		private List<string> boneNamesModernUsed = new List<string>();
		private List<string> boneNamesLegacyUsed = new List<string>();
		private bool scaleEnabled = false;
		private bool reset = false;

		private EditMode editMode;

		public void Show(BodyScale scale)
		{
			editMode = EditMode.Scale;
			Configuration config = Plugin.Configuration;
			EditInterface editWnd = Plugin.InterfaceManager.Show<EditInterface>();
			editWnd.Scale = scale;
			editWnd.ScaleUpdated = scale;
			if (scale == null)
			{
				scale = new BodyScale();
			}

			editWnd.ScaleUpdated = scale;
			editWnd.originalScaleName = scale.ScaleName;
			editWnd.originalScaleCharacter = scale.CharacterName;
			editWnd.newScaleCharacter = scale.CharacterName;

			editWnd.scaleEnabled = scale.BodyScaleEnabled;

			for (int i = 0; i < editWnd.boneNamesLegacy.Count && i < editWnd.boneNamesModern.Count; i++)
			{
				PluginLog.Debug($"Loading bone {i}: {editWnd.boneNamesLegacy[i]}");

				BoneEditsContainer tempContainer = new BoneEditsContainer { Position = MathHelpers.ZeroVector, Rotation = MathHelpers.ZeroVector, Scale = MathHelpers.OneVector };
				if (scale.Bones.TryGetValue(editWnd.boneNamesLegacy[i], out tempContainer))
				{
					PluginLog.Debug($"Found scale");
					editWnd.boneValuesOriginal.Add(editWnd.boneNamesLegacy[i], tempContainer);
					editWnd.boneValuesNew.Add(editWnd.boneNamesLegacy[i], tempContainer);
					editWnd.boneNamesModernUsed.Add(editWnd.boneNamesModern[i]);
					editWnd.boneNamesLegacyUsed.Add(editWnd.boneNamesLegacy[i]);
				}
			}

			editWnd.rootEditsContainer = scale.Bones["n_root"];

			editWnd.originalScaleName = scale.ScaleName;
			editWnd.originalScaleCharacter = scale.CharacterName;
			editWnd.newScaleName = editWnd.originalScaleName;
			editWnd.newScaleCharacter = editWnd.originalScaleCharacter;

			editWnd.scaleIndex = -1;
		}

		protected override void DrawContents()
		{
			Configuration config = Plugin.Configuration;

			string newScaleNameTemp = this.newScaleName;
			string newScaleCharacterTemp = this.newScaleCharacter;
			bool enabledTemp = this.scaleEnabled;
			bool resetTemp = this.reset;

			if (ImGui.Checkbox("Enable", ref enabledTemp))
			{
				this.scaleEnabled = enabledTemp;
				if (config.AutomaticEditMode)
				{
					AddToConfig(this.newScaleName, this.newScaleCharacter);
					config.Save();
					Plugin.LoadConfig(true);
				}
			}

			ImGui.SameLine();

			ImGui.SetNextItemWidth(200);

			if (ImGui.InputText("Character Name", ref newScaleCharacterTemp, 1024))
			{
				this.newScaleCharacter = newScaleCharacterTemp;
			}

			ImGui.SameLine();

			ImGui.SetNextItemWidth(300);
			if (ImGui.InputText("Scale Name", ref newScaleNameTemp, 1024))
			{
				this.newScaleName = newScaleNameTemp;
			}

			ImGui.SameLine();

			bool autoModeEnable = config.AutomaticEditMode;
			if (ImGui.Checkbox("Automatic Mode", ref autoModeEnable))
			{
				config.AutomaticEditMode = autoModeEnable;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Applies changes automatically without saving.");

			if (ImGui.RadioButton("Position", editMode == EditMode.Position))
				editMode = EditMode.Position;

			ImGui.SameLine();
			if (ImGui.RadioButton("Rotation", editMode == EditMode.Rotation))
				editMode = EditMode.Rotation;

			ImGui.SameLine();
			if (ImGui.RadioButton("Scale", editMode == EditMode.Scale))
				editMode = EditMode.Scale;

			ImGui.Separator();

			if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Recycle))
			{
				this.rootEditsContainer = new BoneEditsContainer();
				if (config.AutomaticEditMode)
				{
					this.UpdateCurrent("n_root", this.rootEditsContainer, config.AutomaticEditMode);
				}
				this.reset = true;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Reset");

			ImGui.SameLine();

			Vector3 rootLocalTemp = MathHelpers.OneVector;
			float rootScaleValueAllAxes = 1; //value used for scale when user just want to scale all axes
			bool isRootControlDisabled = false;
			switch (editMode)
			{
				case EditMode.Position:
					rootLocalTemp = rootEditsContainer.Position;
					rootScaleValueAllAxes = 0;
					break;
				case EditMode.Rotation:
					rootLocalTemp = MathHelpers.ZeroVector;
					rootScaleValueAllAxes = 0;
					isRootControlDisabled = true;
					break;
				case EditMode.Scale:
					rootLocalTemp = rootEditsContainer.Scale;
					rootScaleValueAllAxes = (rootLocalTemp.X == rootLocalTemp.Y && rootLocalTemp.X == rootLocalTemp.Z && rootLocalTemp.Y == rootLocalTemp.Z) ? rootLocalTemp.X : 0;
					break;
			}

			if (isRootControlDisabled)
				ImGui.BeginDisabled();

			ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 425);
			if (ImGui.DragFloat3("##Root", ref rootLocalTemp, 0.001f, 0f, 10f))
			{
				if (this.reset)
				{
					rootLocalTemp = new Vector3(1f, 1f, 1f);
					rootScaleValueAllAxes = 1;
					this.reset = false;
				}
				else if (!(rootLocalTemp.X == rootLocalTemp.Y && rootLocalTemp.X == rootLocalTemp.Z && rootLocalTemp.Y == rootLocalTemp.Z))
				{
					rootScaleValueAllAxes = 0;
				}
				/*else if (rootScaleValueAllAxes != 0)
				{
					rootLocalTemp.X = rootScaleValueAllAxes;
					rootLocalTemp.Y = rootScaleValueAllAxes;
					rootLocalTemp.Z = rootScaleValueAllAxes;
				}*/

				switch (editMode)
				{
					case EditMode.Position:
						rootEditsContainer.Position = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
						break;
					case EditMode.Rotation:
						rootEditsContainer.Rotation = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
						break;
					case EditMode.Scale:
						rootEditsContainer.Scale = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
						break;
				}

				if (config.AutomaticEditMode)
				{
					this.UpdateCurrent("n_root", this.rootEditsContainer, config.AutomaticEditMode);
				}
			}
			if (isRootControlDisabled)
				ImGui.EndDisabled();

			ImGui.SameLine();
			if(editMode != EditMode.Scale)
				ImGui.BeginDisabled();

			ImGui.SetNextItemWidth(100);
			if (ImGui.DragFloat("##RootAllAxes", ref rootScaleValueAllAxes, 0.001f, 0f, 10f))
			{
				if (rootScaleValueAllAxes != 0)
				{
					rootLocalTemp.X = rootScaleValueAllAxes;
					rootLocalTemp.Y = rootScaleValueAllAxes;
					rootLocalTemp.Z = rootScaleValueAllAxes;
				}

				rootEditsContainer.Scale = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);

				if (config.AutomaticEditMode)
				{
					this.UpdateCurrent("n_root", this.rootEditsContainer, config.AutomaticEditMode);
				}
			}

			if (editMode != EditMode.Scale)
				ImGui.EndDisabled();

			ImGui.SameLine();
			ImGui.Text("Root");

			string col1Label = "X";
			string col2Label = "Y";
			string col3Label = "Z";
			string col4Label = "All";

			switch (editMode)
			{
				case EditMode.Rotation:
					col1Label = "Roll";
					col2Label = "Yaw";
					col3Label = "Pitch";
					break;
			}

			ImGui.Separator();
			ImGui.BeginTable("Bones", 6, ImGuiTableFlags.SizingStretchSame);
			ImGui.TableNextColumn();
			ImGui.Text("Bones:");
			ImGui.TableNextColumn();
			ImGui.Text(col1Label);
			ImGui.TableNextColumn();
			ImGui.Text(col2Label);
			ImGui.TableNextColumn();
			ImGui.Text(col3Label);
			ImGui.TableNextColumn();
			ImGui.Text(col4Label);
			ImGui.TableNextColumn();
			ImGui.Text("Name");
			ImGui.EndTable();

			ImGui.BeginChild("scrolling", new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56), false);

			for (int i = 0; i < boneValuesNew.Count; i++)
			{
				string boneNameLocalLegacy = this.boneNamesLegacyUsed[i];

				string boneNameLocalModern = this.boneNamesModernUsed[i];

				ImGui.PushID(i);

				if (!this.IsBoneNameEditable(boneNameLocalModern))
				{
					ImGui.PopID();
					continue;
				}

				BoneEditsContainer currentEditsContainer = new BoneEditsContainer { Position = MathHelpers.ZeroVector, Rotation = MathHelpers.ZeroVector, Scale = MathHelpers.OneVector };
				string label = "Not Found";

				try
				{
					if (this.boneValuesNew.TryGetValue(boneNameLocalLegacy, out currentEditsContainer))
						label = boneNameLocalModern;
					else if (this.boneValuesNew.TryGetValue(boneNameLocalModern, out currentEditsContainer))
						label = boneNameLocalModern;
					else
						currentEditsContainer = new BoneEditsContainer { Position = MathHelpers.ZeroVector, Rotation = MathHelpers.ZeroVector, Scale = MathHelpers.OneVector };
				}
				catch (Exception ex)
				{

				}

				Vector3 currentVector = MathHelpers.OneVector;
				float currentScaleValueAllAxes = 1; //value used for scale when user just want to scale all axes
				switch (editMode)
				{
					case EditMode.Position:
						currentVector = currentEditsContainer.Position;
						break;
					case EditMode.Rotation:
						currentVector = currentEditsContainer.Rotation;
						break;
					case EditMode.Scale:
						currentVector = currentEditsContainer.Scale;
						currentScaleValueAllAxes = (currentVector.X == currentVector.Y && currentVector.X == currentVector.Z && currentVector.Y == currentVector.Z) ? currentVector.X : 0;
						break;
				}

				if (ImGuiComponents.IconButton(i, FontAwesomeIcon.Recycle))
				{
					this.reset = true;
				}

				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"Reset");

				if (this.reset)
				{
					BoneEditsContainer editsContainer = null;

					switch (editMode)
					{
						case EditMode.Position:
						case EditMode.Rotation:
							currentScaleValueAllAxes = 0;
							currentVector.X = 0F;
							currentVector.Y = 0F;
							currentVector.Z = 0F;
							break;
						case EditMode.Scale:
							currentScaleValueAllAxes = 1;
							currentVector.X = 1F;
							currentVector.Y = 1F;
							currentVector.Z = 1F;
							break;
					}
					this.reset = false;
					try
					{
						if (this.boneValuesNew.ContainsKey(boneNameLocalModern))
							editsContainer = this.boneValuesNew[boneNameLocalModern];
						else if (this.boneValuesNew.Remove(boneNameLocalLegacy, out BoneEditsContainer removedContainer))
						{
							editsContainer = removedContainer;
							this.boneValuesNew[boneNameLocalLegacy] = editsContainer;
						}
						else
							throw new Exception();

						switch (editMode)
						{
							case EditMode.Position:
								editsContainer.Position = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case EditMode.Rotation:
								editsContainer.Rotation = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case EditMode.Scale:
								editsContainer.Scale = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
						}
					}
					catch
					{
						//throw new Exception();
					}
					if (config.AutomaticEditMode)
					{
						this.UpdateCurrent(boneNameLocalLegacy, editsContainer, config.AutomaticEditMode);
					}
				}
				else if (currentVector.X == currentVector.Y && currentVector.Y == currentVector.Z)
				{
					currentScaleValueAllAxes = currentVector.X;
				}
				else
				{
					currentScaleValueAllAxes = 0;
				}

				ImGui.SameLine();

				ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 400);

				float minLimit = -10f;
				float maxLimit = 10f;
				float increment = 0.001f;

				switch (editMode)
				{
					case EditMode.Rotation:
						minLimit = -360f;
						maxLimit = 360f;
						increment = 1f;
						break;
				}

				if (ImGui.DragFloat3($"##{label}", ref currentVector, increment, minLimit, maxLimit))
				{
					BoneEditsContainer editsContainer = null;
					try
					{
						if (this.reset)
						{
							switch (editMode)
							{
								case EditMode.Position:
								case EditMode.Rotation:
									currentScaleValueAllAxes = 0F;
									currentVector.X = 0F;
									currentVector.Y = 0F;
									currentVector.Z = 0F;
									break;
								case EditMode.Scale:
									currentScaleValueAllAxes = 1F;
									currentVector.X = 1F;
									currentVector.Y = 1F;
									currentVector.Z = 1F;
									break;
							}
							this.reset = false;
						}
						else if (!((currentVector.X == currentVector.Y) && (currentVector.X == currentVector.Z) && (currentVector.Y == currentVector.Z)))
						{
							currentScaleValueAllAxes = 0;
						}
						/*else if (currentVector.W != 0)
						{
							currentVector.X = currentVector.W;
							currentVector.Y = currentVector.W;
							currentVector.Z = currentVector.W;
						}*/
					}
					catch (Exception ex)
					{

					}
					try
					{
						if (this.boneValuesNew.ContainsKey(boneNameLocalModern))
							editsContainer = this.boneValuesNew[boneNameLocalModern];
						else if (this.boneValuesNew.Remove(boneNameLocalLegacy, out BoneEditsContainer removedContainer))
						{
							editsContainer = removedContainer;
							this.boneValuesNew[boneNameLocalLegacy] = editsContainer;
						}
						else
							throw new Exception();

						switch (editMode)
						{
							case EditMode.Position:
								editsContainer.Position = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case EditMode.Rotation:
								editsContainer.Rotation = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case EditMode.Scale:
								editsContainer.Scale = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
						}
					}
					catch
					{
						//throw new Exception();
					}
					if (config.AutomaticEditMode)
					{
						this.UpdateCurrent(boneNameLocalLegacy, editsContainer, config.AutomaticEditMode);
					}
				}

				ImGui.SameLine();
				if (editMode != EditMode.Scale)
					ImGui.BeginDisabled();

				ImGui.SetNextItemWidth(100);
				if (ImGui.DragFloat($"##{label}AllAxes", ref currentScaleValueAllAxes, 0.001f, 0f, 10f))
				{
					if (currentScaleValueAllAxes != 0)
					{
						currentVector.X = currentScaleValueAllAxes;
						currentVector.Y = currentScaleValueAllAxes;
						currentVector.Z = currentScaleValueAllAxes;
					}

					BoneEditsContainer editsContainer = null;
					if (this.boneValuesNew.ContainsKey(boneNameLocalModern))
						editsContainer = this.boneValuesNew[boneNameLocalModern];
					else if (this.boneValuesNew.Remove(boneNameLocalLegacy, out BoneEditsContainer removedContainer))
					{
						editsContainer = removedContainer;
						this.boneValuesNew[boneNameLocalLegacy] = editsContainer;
					}
					else
						throw new Exception();

					editsContainer.Scale = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);

					if (config.AutomaticEditMode)
					{
						this.UpdateCurrent(boneNameLocalLegacy, editsContainer, config.AutomaticEditMode);
					}
				}

				if (editMode != EditMode.Scale)
					ImGui.EndDisabled();

				ImGui.SameLine();
				ImGui.Text(label);

				ImGui.PopID();
			}

			ImGui.EndChild();

			ImGui.Separator();

			if (ImGui.Button("Save"))
			{
				AddToConfig(this.newScaleName, this.newScaleCharacter);
				if (this.newScaleCharacter != this.originalScaleCharacter)
					this.originalScaleCharacter = this.newScaleCharacter;
				if (this.newScaleName != this.originalScaleName)
					this.originalScaleName = this.newScaleName;
				config.Save();
				Plugin.LoadConfig();
			}

			/* TODO feature: undo of some variety. Option below is a revert to what was present when edit was opened, but needs additonal logic
			 * ImGui.SameLine();
			if (ImGui.Button("Revert"))
			{
				RevertToOriginal();
				//config.Save();
			}
			*/

			ImGui.SameLine();

			if (ImGui.Button("Save and Close"))
			{
				AddToConfig(this.newScaleName, this.newScaleCharacter);
				config.Save();
				Plugin.LoadConfig();
				this.Close();
			}
			ImGui.SameLine();
			if (ImGui.Button("Cancel"))
			{
				this.Close();
			}

			ImGui.SameLine();

			ImGui.Text("    Save and close with new scale name or new character name to create a copy.");
		}

		private void AddToConfig(string scaleName, string characterName)
		{
			Configuration config = Plugin.Configuration;
			BodyScale newBody = new BodyScale();

			for (int i = 0; i < this.boneNamesLegacy.Count && i < this.boneValuesNew.Count; i++)
			{
				string legacyName = boneNamesLegacyUsed[i];

				newBody.Bones[legacyName] = this.boneValuesNew[legacyName];
			}

			newBody.Bones["n_root"] = this.rootEditsContainer;

			newBody.BodyScaleEnabled = this.scaleEnabled;
			newBody.ScaleName = scaleName;
			newBody.CharacterName = characterName;

			//newBody.RootScale = new HkVector4(this.newRootScale.X, this.newRootScale.Y, this.newRootScale.Z, 0);
			if (this.originalScaleName == scaleName && this.originalScaleCharacter == characterName)
			{
				int matchIndex = -1;
				for (int i = 0; i < config.BodyScales.Count; i++)
				{
					if (config.BodyScales[i].ScaleName == scaleName && config.BodyScales[i].CharacterName == characterName)
					{
						matchIndex = i;
						break;
					}
				}
				if (matchIndex >= 0)
				{
					config.BodyScales.RemoveAt(matchIndex);
					config.BodyScales.Insert(matchIndex, newBody);
				}
			}
			else
			{
				this.originalScaleName = scaleName;
				this.originalScaleCharacter = characterName;
				config.BodyScales.Add(newBody);
				if (this.scaleEnabled)
				{
					config.ToggleOffAllOtherMatching(characterName, scaleName);
				}
			}
		}

		/*private void RevertToOriginal() //Currently Unused
		{
			this.boneValuesNew = this.boneValuesOriginal;
			this.newRootScale = this.originalRootScale;
		}*/

		private void UpdateCurrent(string boneName, BoneEditsContainer boneValue, bool autoMode = false)
		{
			Configuration config = Plugin.Configuration;
			BodyScale newBody = this.ScaleUpdated;

			newBody.Bones[boneName] = boneValue;

			if (this.scaleIndex == -1 || this.scaleIndex > config.BodyScales.Count)
			{
				this.scaleIndex = GetCurrentScaleIndex(this.originalScaleName, this.originalScaleCharacter);
			}

			config.BodyScales[this.scaleIndex] = newBody;
			config.Save();
			Plugin.LoadConfig(autoMode);
		}

		private int GetCurrentScaleIndex(string scaleName, string scaleCharacter)
		{
			Configuration config = Plugin.Configuration;
			int matchIndex = -1;
			for (int i = 0; i < config.BodyScales.Count; i++)
			{
				if (config.BodyScales[i].ScaleName == scaleName && config.BodyScales[i].CharacterName == scaleCharacter)
				{
					matchIndex = i;
					break;
				}
			}
			if (matchIndex >= 0)
			{
				return matchIndex;
			}
			return -1;
		}

		private bool IsBoneNameEditable(string boneNameModern)
		{
			// Megahack method
			if (boneNameModern == "Root" || boneNameModern == "Throw" || boneNameModern == "Abdomen" 
				|| boneNameModern.Contains("Cloth") || boneNameModern.Contains("Scabbard") || boneNameModern.Contains("Pauldron")
				|| boneNameModern.Contains("Holster") || boneNameModern.Contains("Poleyn") || boneNameModern.Contains("Shield")
				|| boneNameModern.Contains("Couter") || boneNameModern.Contains("Weapon") || boneNameModern.Contains("Sheathe"))
				return false;
			return true;
		}
	}

	public enum EditMode
	{
		Position,
		Rotation,
		Scale
	}
}
