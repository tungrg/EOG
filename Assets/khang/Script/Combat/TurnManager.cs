using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    private List<ICombatant> allCombatants = new List<ICombatant>();

    public void Initialize(List<Combatant> players, List<Enemy> enemies)
    {
        allCombatants.Clear();
        allCombatants.AddRange(players.Cast<ICombatant>());
        allCombatants.AddRange(enemies.Cast<ICombatant>());

        if (allCombatants.Count == 0) return;

        float maxSPD = allCombatants.Max(c => c.SPD);

        foreach (var combatant in allCombatants)
        {
            // Khởi tạo AV theo công thức: AV = 10000 * (SPD / MaxSPD)
            float initialAV = 10000f * (combatant.SPD / maxSPD);
            combatant.ActionValue = initialAV;
            //DebugLogger.Log($"[INIT] {combatant.Name} initial AV: {combatant.ActionValue:F1} (SPD: {combatant.SPD}, MaxSPD: {maxSPD})");
        }
    }

    public (ICombatant, bool)? ProcessNextTick()
    {
        var liveCombatants = allCombatants.Where(c => c != null && c.HP > 0).ToList();
        if (liveCombatants.Count == 0)
        {
            DebugLogger.Log("[TURN] No live combatants remaining.");
            return null;
        }

        //DebugLogger.Log("[TICK] Before tick: " + string.Join(", ", liveCombatants.Select(c => $"{c.Name} AV: {c.ActionValue:F1}")));

        // Tăng AV cho tất cả nhân vật còn sống (1 tick logic)
        foreach (var combatant in liveCombatants)
        {
            combatant.AdvanceActionValue(combatant.SPD);
            //DebugLogger.Log($"[ADVANCE] {combatant.Name} AV increased by {combatant.SPD:F1} to {combatant.ActionValue:F1}");
        }

        // Tìm nhân vật có AV >= 10000 để hành động
        ICombatant nextCombatant = liveCombatants
            .Where(c => c.ActionValue >= 10000f)
            .OrderByDescending(c => c.ActionValue)
            .ThenByDescending(c => c.SPD)
            .FirstOrDefault();

        if (nextCombatant == null)
        {
            //DebugLogger.Log("[TICK] No combatant has AV >= 10000 yet.");
            return null;
        }

        bool isPlayer = nextCombatant is Combatant;
        string color = isPlayer ? "#00B7EB" : "#FF0000";
        string avFormatted = nextCombatant.ActionValue.ToString("F1").Replace(".", ",");
        DebugLogger.Log($"<color={color}>[TURN] Next: {nextCombatant.Name} (SPD:{nextCombatant.SPD} | AV:{avFormatted})</color>");

        // Reset AV sau khi hành động
        nextCombatant.ActionValue = 0f;
        //DebugLogger.Log($"[RESET] {nextCombatant.Name} AV reset to: {nextCombatant.ActionValue:F1}");

        // Kiểm tra và sửa AV âm nếu có
        foreach (var c in liveCombatants)
        {
            if (c.ActionValue < 0)
            {
                c.ActionValue = 0f;
                DebugLogger.LogWarning($"[FIX] {c.Name} had negative AV, reset to: {c.ActionValue:F1}");
            }
        }

        //DebugLogger.Log("[TICK] After tick: " + string.Join(", ", liveCombatants.Select(c => $"{c.Name} AV: {c.ActionValue:F1}")));
        return (nextCombatant, isPlayer);
    }
}