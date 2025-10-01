using UnityEngine;

public static class DebugLogger
{
    public static void Log(string message, ICombatant combatant = null)
    {
        if (combatant != null)
        {
            string color = combatant is Combatant ? "#00B7EB" : "#FF0000";
            message = message.Replace(combatant.Name, $"<color={color}>{combatant.Name}</color>");
        }
        Debug.Log($"[{System.DateTime.Now:HH:mm:ss}] {message}");
    }

    public static void LogWarning(string message, ICombatant combatant = null)
    {
        if (combatant != null)
        {
            string color = combatant is Combatant ? "#00B7EB" : "#FF0000";
            message = message.Replace(combatant.Name, $"<color={color}>{combatant.Name}</color>");
        }
        Debug.LogWarning($"[{System.DateTime.Now:HH:mm:ss}] {message}");
    }

    public static void LogError(string message, ICombatant combatant = null)
    {
        if (combatant != null)
        {
            string color = combatant is Combatant ? "#00B7EB" : "#FF0000";
            message = message.Replace(combatant.Name, $"<color={color}>{combatant.Name}</color>");
        }
        Debug.LogError($"[{System.DateTime.Now:HH:mm:ss}] {message}");
    }
}