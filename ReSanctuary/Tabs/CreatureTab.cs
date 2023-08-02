using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using ReSanctuary.Windows;

namespace ReSanctuary.Tabs;

public class CreatureTab : MainWindowTab {
    private string filter = string.Empty;

    public CreatureTab(Plugin plugin) : base(plugin, "Creatures") { }

    public override void Draw() {
        var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit |
                         ImGuiTableFlags.NoKeepColumnsVisible;

        ImGui.Text("The data on this tab is crowdsourced and may not be accurate or complete.");

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputText(string.Empty, ref this.filter, 256);

        if (ImGui.BeginTable("ReSanctuary_MainWindowTable", 7, tableFlags)) {
            ImGui.TableSetupColumn("Size");
            ImGui.TableSetupColumn("Icon");
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Posistion");
            ImGui.TableSetupColumn("Spawn Requirements");
            ImGui.TableSetupColumn("Guaranteed Drop");
            ImGui.TableSetupColumn("Chance of Drop");

            ImGui.TableHeadersRow();

            foreach (var item in this.Plugin.CreatureItems) {
                var nameMatches = item.Name.ToLower().Contains(this.filter.ToLower());
                var oneMatches = item.Item1ShortName.ToLower().Contains(this.filter.ToLower());
                var twoMatches = item.Item2ShortName.ToLower().Contains(this.filter.ToLower());

                var weather = item.ExtraData?.Weather;
                var weatherMatches = false;
                if (weather != null && this.Plugin.WeatherList.TryGetValue(weather.Value, out var weatherData)) {
                    var name = weatherData.Name.ToDalamudString().TextValue;
                    weatherMatches = name.ToLower().Contains(this.filter.ToLower());
                }

                if (!nameMatches && !oneMatches && !twoMatches && !weatherMatches) continue;

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                var size = item.Size switch {
                    1 => "[S]",
                    2 => "[M]",
                    3 => "[L]",
                    _ => "[?]"
                };
                ImGui.Text(size);

                ImGui.TableSetColumnIndex(1);
                var iconSize = ImGui.GetTextLineHeight() * 1.5f;
                var iconSizeVec = new Vector2(iconSize, iconSize);
                ImGui.Image(Utils.GetFromIconCache(item.IconId).ImGuiHandle, iconSizeVec, Vector2.Zero, Vector2.One);

                ImGui.TableSetColumnIndex(2);
                var itemName = item.ExtraData?.Name;
                if (itemName != null) {
                    ImGui.Text(itemName);
                } else {
                    ImGui.TextDisabled("???");
                }

                ImGui.TableSetColumnIndex(3);

                if (item.ExtraData != null) {
                    ImGui.Text(item.ExtraData.InGameX.ToString("F1") + ", " + item.ExtraData.InGameY.ToString("F1"));
                    ImGui.SameLine();
                    ImGui.PushID("ReSanctuary_CreatureMap_" + (int) item.CreatureId);
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.MapMarkerAlt)) {
                        var teri = Plugin.IslandSanctuary.RowId;

                        PluginLog.Debug("radius: {radius}", item.ExtraData.Radius);

                        Utils.OpenGatheringMarker(teri, (int) item.MarkerX, (int) item.MarkerZ, item.ExtraData.Radius,
                                                  item.Name);
                    }
                    ImGui.PopID();
                } else {
                    ImGui.TextDisabled("???");
                }


                ImGui.TableSetColumnIndex(4);
                //spawn limits
                if (item.ExtraData != null) {
                    if (item.ExtraData.Weather != null) {
                        var weatherEntry = this.Plugin.WeatherList[item.ExtraData.Weather.Value];
                        var weatherSize = ImGui.GetTextLineHeight() * 1.25f;
                        var weatherSizeVec = new Vector2(weatherSize, weatherSize);
                        var weatherIcon = weatherEntry.Icon;
                        ImGui.Image(Utils.GetFromIconCache((uint) weatherIcon).ImGuiHandle, weatherSizeVec,
                                    Vector2.Zero, Vector2.One);
                        ImGui.SameLine();
                        ImGui.Text(weatherEntry.Name);
                    }

                    if (item.ExtraData.SpawnStart != null && item.ExtraData.SpawnEnd != null) {
                        if (item.ExtraData.Weather != null) ImGui.SameLine();

                        var start = Utils.Format24HourAsAmPm(item.ExtraData.SpawnStart.Value);
                        var end = Utils.Format24HourAsAmPm(item.ExtraData.SpawnEnd.Value);
                        ImGui.TextUnformatted(start + " - " + end);
                    }
                } else {
                    ImGui.TextDisabled("Unknown");
                }

                ImGui.TableSetColumnIndex(5);

                {
                    var oneIconSize = ImGui.GetTextLineHeight() * 1.5f;
                    var oneIconSizeVec = new Vector2(oneIconSize, oneIconSize);

                    ImGui.Image(Utils.GetFromIconCache(item.Item1.Icon).ImGuiHandle, oneIconSizeVec, Vector2.Zero,
                                Vector2.One);
                    ImGui.SameLine();
                    ImGui.Text(item.Item1ShortName);
                    ImGui.SameLine();
                    ImGui.PushID("ReSanctuary_CreatureItem_" + (int) item.CreatureId + "_" + (int) item.Item1Id);

                    if (ImGuiComponents.IconButton(FontAwesomeIcon.ClipboardList)) {
                        var rowId = this.Plugin.MJIItemPouchSheet.First(x => {
                            var itemId = x.ReadColumn<uint>(0);
                            var itemValue = this.Plugin.ItemSheet.GetRow(itemId);
                            if (itemId == 0 || itemValue == null) return false;

                            return itemValue.RowId == item.Item1Id;
                        }).RowId;

                        Utils.AddToTodoList(Plugin.Configuration, rowId);
                    }
                }

                ImGui.PopID();
                ImGui.TableSetColumnIndex(6);

                {
                    var twoIconSize = ImGui.GetTextLineHeight() * 1.5f;
                    var twoIconSizeVec = new Vector2(twoIconSize, twoIconSize);

                    ImGui.Image(Utils.GetFromIconCache(item.Item2.Icon).ImGuiHandle, twoIconSizeVec,
                                Vector2.Zero, Vector2.One);
                    ImGui.SameLine(0);
                    ImGui.Text(item.Item2ShortName);
                    ImGui.SameLine();
                    ImGui.PushID("ReSanctuary_CreatureItem_" + (int) item.CreatureId + "_" + (int) item.Item2Id);

                    if (ImGuiComponents.IconButton(FontAwesomeIcon.ClipboardList)) {
                        var rowId = this.Plugin.MJIItemPouchSheet.First(x => {
                            var itemId = x.ReadColumn<ushort>(0);
                            var itemValue = this.Plugin.ItemSheet.GetRow(itemId);
                            if (itemId == 0 || itemValue == null) return false;

                            return itemValue.RowId == item.Item2Id;
                        }).RowId;

                        Utils.AddToTodoList(Plugin.Configuration, rowId);
                    }
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }
}
