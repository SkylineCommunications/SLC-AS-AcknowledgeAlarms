// <copyright file="Acknowledge Alarms.cs" company="Skyline Communications">
// Copyright (c) Skyline Communications. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.Messages;


/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
    /// <summary>
    /// The Script entry point.
    /// </summary>
    /// <param name="engine">Link with SLAutomation process.</param>
    public void Run(Engine engine)
    {
        // Set required engine flags
        engine.SetFlag(RunTimeFlags.NoKeyCaching);
        engine.SetFlag(RunTimeFlags.AllowUndef);

        try
        {
            // Parse the alarm parameters
            ScriptParam alarms = engine.GetScriptParam("alarms");
            string alarmsParameters = alarms.Value;
            engine.Log($"Processing alarm parameters: {alarmsParameters}");

            // Split and validate parameters
            string[] parameters = alarmsParameters.Split(new[] { "&&" }, StringSplitOptions.None);
            if (parameters.Length < 4)
            {
                throw new ArgumentException("Insufficient alarm parameters provided. Expected format: dataminerId&&elementId&&alarmId&&rootId");
            }

            int numberOfAlarms = parameters[0].Count(c => c == '+');

            List<Alarm> alarmsToAcknowledge = new List<Alarm>();
            if (numberOfAlarms > 0)
            {
                engine.Log($"Multiple alarms detected {numberOfAlarms + 1}");

                // Split the strings first
                string[] dataminerStrArr = parameters[0].Split('+');
                string[] elementStrArr = parameters[1].Split('+');
                string[] alarmStrArr = parameters[2].Split('+');
                string[] rootStrArr = parameters[3].Split('+');

                // Convert to int arrays
                int[] dataminerIdArr = dataminerStrArr.Select(int.Parse).ToArray();
                int[] elementIdArr = elementStrArr.Select(int.Parse).ToArray();
                int[] alarmIdArr = alarmStrArr.Select(int.Parse).ToArray();
                int[] rootIdArr = rootStrArr.Select(int.Parse).ToArray();

                // Validate array lengths
                if (dataminerIdArr.Length != elementIdArr.Length ||
                    elementIdArr.Length != alarmIdArr.Length ||
                    alarmIdArr.Length != rootIdArr.Length)
                {
                    throw new ArgumentException("All alarm parameters must have the same number of elements.");
                }

                for (int i = 0; i < dataminerIdArr.Length; i++)
                {
                    // Create and add alarm to the list
                    engine.Log($"Adding alarm: {dataminerIdArr[i]}/{elementIdArr[i]}/{alarmIdArr[i]} - RootId: {rootIdArr[i]}");
                    alarmsToAcknowledge.Add(new Alarm(dataminerIdArr[i], elementIdArr[i], alarmIdArr[i], rootIdArr[i]));
                }
            }
            else
            {
                // Parse alarm identifiers
                int dataminerId = Convert.ToInt32(parameters[0]);
                int elementId = Convert.ToInt32(parameters[1]);
                int alarmId = Convert.ToInt32(parameters[2]);
                int rootId = Convert.ToInt32(parameters[3]);
                Alarm alarm = new Alarm(dataminerId, elementId, alarmId, rootId);
                alarmsToAcknowledge.Add(alarm);
            }

            int failedAcks = 0;

            foreach (Alarm alarm in alarmsToAcknowledge)
            {
                try
                {
                    AcknowledgeAlarms(engine, alarm);
                }
                catch (Skyline.DataMiner.Net.Exceptions.DataMinerCOMException ex)
                {
                    failedAcks++;
                    engine.Log($"Acknowledge failed for {alarm.DataminerId}/{alarm.ElementId}/{alarm.AlarmId}: {ex.Message}");
                }
            }

            if (failedAcks > 0)
            {
                engine.Log($"Alarm acknowledgment completed with {failedAcks} failure(s).");
            }
            else
            {
                engine.Log("Alarm acknowledgment completed successfully");
            }
        }
        catch (FormatException ex)
        {
            engine.Log($"Parameter parsing error: {ex.Message}");
            engine.ExitFail($"Invalid parameter format: {ex.Message}");
        }
        catch (Exception ex)
        {
            engine.Log($"Alarm acknowledgment failed: {ex.Message}");
            engine.ExitFail(ex.ToString());
        }
    }

    private void AcknowledgeAlarms(Engine engine, Alarm alarmToAcknowledge)
    {
        // Create AlarmTreeID
        var treeId = new AlarmTreeID(alarmToAcknowledge.DataminerId, alarmToAcknowledge.ElementId, alarmToAcknowledge.AlarmId);
        engine.Log($"Acknowledging alarm: {alarmToAcknowledge.DataminerId}/{alarmToAcknowledge.ElementId}/{alarmToAcknowledge.AlarmId} - TreeId: {treeId}");

        // Request alarm details
        var getAlarmDetails = new GetAlarmDetailsMessage(new[] { treeId });
        var alarmResponse = Engine.SLNet.SendSingleResponseMessage(getAlarmDetails) as AlarmEventMessage;

        // Validate response
        if (alarmResponse == null)
        {
            throw new InvalidOperationException($"Failed to retrieve alarm details for TreeId: {treeId}");
        }

        // Log alarm info
        engine.Log(
            $"Alarm details: ID={alarmResponse.AlarmID}, Source={alarmResponse.Source}, " +
            $"Description={alarmResponse.Description}, Type={alarmResponse.Type}, " +
            $"Value={alarmResponse.Value}, CorrelationCount={alarmResponse.CorrelationBaseAlarmReferences?.Length ?? 0}");

        // Acknowledge correlated alarms if applicable
        if (alarmResponse.Source?.Equals("Correlation Engine", StringComparison.OrdinalIgnoreCase) == true &&
            alarmResponse.CorrelationBaseAlarmReferences != null)
        {
            engine.Log("Processing correlated alarms...");

            foreach (var correlatedAlarm in alarmResponse.CorrelationBaseAlarmReferences)
            {
                try
                {
                    string ackMessage = $"Acknowledged by {treeId}";
                    engine.Log($"Acknowledging base alarm: {correlatedAlarm}");
                    engine.AcknowledgeAlarm(correlatedAlarm, ackMessage);
                }
                catch (Skyline.DataMiner.Net.Exceptions.DataMinerCOMException ex)
                {
                    engine.Log($"Failed to acknowledge base alarm {correlatedAlarm}: {ex.Message}");
                }
            }
        }

        // Acknowledge the main alarm
        try
        {
            engine.AcknowledgeAlarm(treeId, "Acknowledged via script");
        }
        catch (Skyline.DataMiner.Net.Exceptions.DataMinerCOMException ex)
        {
            engine.Log($"Failed to acknowledge main alarm {treeId}: {ex.Message}");
            throw;
        }
    }

    public class Alarm
    {
        public int DataminerId { get; set; }

        public int ElementId { get; set; }

        public int AlarmId { get; set; }

        public int RootId { get; set; }

        public Alarm()
        {
        }

        public Alarm(int dataminerId, int elementId, int alarmId, int rootId)
        {
            DataminerId = dataminerId;
            ElementId = elementId;
            AlarmId = alarmId;
            RootId = rootId;
        }
    }
}
