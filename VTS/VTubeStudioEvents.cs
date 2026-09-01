using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using VoiceTrigger.Logging;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS;

public sealed class VTubeStudioEvents
{
    readonly record struct HandlerEntry(JsonEventHandler Handler, HashSet<Delegate> Delegates);
    public delegate void EventHandler<T>(T e) where T : VTSResponseTemplate;
    delegate void JsonEventHandler(HashSet<Delegate> handlers, JsonDocument doc);
    readonly Dictionary<string, Type> Mappings = [];
    readonly Dictionary<Type, HandlerEntry> Handlers = [];

    public Type this[string messageType] => Mappings[messageType];
    public bool Track<T>(EventHandler<T> handler) where T : VTSResponseTemplate
    {
        if (!Handlers.TryGetValue(typeof(T), out var entry))
        {
            throw new InvalidOperationException($"No message type was mapped to the response type ({typeof(T).Name}) yet!");
        }
        return entry.Delegates.Add(handler);
    }

    public bool Forget<T>(EventHandler<T> handler) where T : VTSResponseTemplate
    {
        if (!Handlers.TryGetValue(typeof(T), out var entry))
        {
            return false;
        }
        return entry.Delegates.Remove(handler);
    }

    public bool TryFire(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty(VTSPackets.MessageTypeJsonPropertyName, out var element))
        {
            $"({VTSPackets.MessageTypeJsonPropertyName}) cannot be found in the input event string! Json payload is ignored."
                .Out(ConsoleColor.Yellow);
            return false;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            $"({VTSPackets.MessageTypeJsonPropertyName}) is not a string! Json payload is ignored.".Out(ConsoleColor.Yellow);
            return false;
        }

        string? messageType = element.GetString();
        if (string.IsNullOrEmpty(messageType)) return false;
        if (Mappings.TryGetValue(messageType, out var type))
        {
            if (Handlers.TryGetValue(type, out var entry))
            {
                try
                {
                    entry.Handler(entry.Delegates, doc);
                }
                catch (Exception ex)
                {
                    ex.Out($"Exception while handling the event for a message type ({messageType})!\n");
                }
                return true;
            }
            else
            {
                $"Bug found! Event handler entry doesn't exist! Json event will be ignored!".Out(ConsoleColor.Yellow);
                return false;
            }
        }
        return false;
    }

    //static class Reflect<T>
    //{
    //    static class IsAssignableFrom<Y>
    //    {
    //        public static readonly bool Result = typeof(T).IsAssignableFrom(typeof(Y));
    //    }
    //}

    public void Map<T>(string messageType) where T : VTSResponseTemplate
    {
        if (!Mappings.TryAdd(messageType, typeof(T)))
        {
            if (Mappings.TryGetValue(messageType, out var existing))
            {
                throw new InvalidOperationException(
                    $"Cannot map event to type ({typeof(T).Name}). MessageType '{messageType}' is already mapped to ({existing.Name})!");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot map event to type ({typeof(T).Name}) for an unknown reason! MessageType: {messageType}");
            }
        }
        else Handlers[typeof(T)] = new(JsonHandler, []);
        static void JsonHandler(HashSet<Delegate> handlers, JsonDocument doc)
        {
            var packet = doc.Deserialize<T>(VTSPackets.JsonOptions);
            if (packet == null)
            {
                $"Deserialized into ({packet}). Json event payload will be discarded, but event will be consumed.".Out(ConsoleColor.Yellow);
                return;
            }

            var delegates = ArrayPool<EventHandler<T>>.Shared.Rent(handlers.Count);
            int amount = handlers.Count;
            handlers.CopyTo(delegates);
            for (int i = 0; i < amount; i++)
            {
                delegates[i](packet);
            }
        }
    }

    public bool UnMap(string messageType)
    {
        if (Mappings.Remove(messageType, out var type))
        {
            Handlers.Remove(type);
            return true;
        }
        return false;
    }

    public bool UnMap<T>(string messageType) where T : VTSRequestTemplate => UnMap(messageType, typeof(T));
    public bool UnMap(string messageType, Type type)
    {
        if (Mappings.TryGetValue(messageType, out var t) && t == type)
        {
            if (Mappings.Remove(messageType))
                Handlers.Remove(type);
            return true;
        }
        return false;
    }

    public bool Contains(string messageType) => Mappings.ContainsKey(messageType);
    public bool TryGet(string messageType, [NotNullWhen(true)] out Type? type) => Mappings.TryGetValue(messageType, out type);
}

/*
// Requesting a list of models.
string? loadedModelID = null;
{
    $"Requesting a list of models...".Out();
    var result = await Request<VTSAvailableModelsResponse>(new VTSAvailableModelsRequest());
    if (result.ResolveSuccess(out var response))
    {
        var item = response.Data?.AvailableModels?.FirstOrDefault(static d => d.ModelLoaded);
        if (item.HasValue && item.Value.ModelLoaded)
        {
            loadedModelID = item.Value.ModelID;
            $"Found an active model! (id: {loadedModelID}, name: {item.Value.ModelName})".Out(ConsoleColor.Green);
        }
        else
        {
            $"No loaded model found!".Out(ConsoleColor.Yellow);
        }
    }
    else
    {
        $"Failed to retrieve a list of models!".Out(ConsoleColor.Yellow);
    }
}

// Requesting a list of hotkeys.
string? targetHotkeyID = null;
if (!string.IsNullOrEmpty(loadedModelID))
{
    $"Requesting a list of hotkeys...".Out();
    var result = await Request<VTSModelHotkeysResponse>(new VTSModelHotkeysRequest()
    {
        Data = new()
        {
            ModelID = loadedModelID,
            Live2DItemFileName = null,
        }
    });
    if (result.ResolveSuccess(out var response))
    {
        var item = response.Data?.AvailableHotkeys?.FirstOrDefault(static d => d.Name == "粉双马尾");
        if (item.HasValue && item.Value.Name == "粉双马尾")
        {
            targetHotkeyID = item.Value.HotkeyID;
            $"Hotkey found! (id: {targetHotkeyID}, name: {item.Value.Name})".Out(ConsoleColor.Green);
        }
        else
        {
            $"Failed to find a target hotkey!".Out();
        }
    }
    else
    {
        $"Failed to retrieve a list of hotkeys!".Out(ConsoleColor.Yellow);
    }
}

// Requesting execution of one of them.
if (!string.IsNullOrEmpty(targetHotkeyID))
{
    $"Requesting a hotkey execution...".Out();
    var result = await Request<VTSHotkeyTriggerResponse>(new VTSHotkeyTriggerRequest()
    {
        Data = new()
        {
            HotkeyID = targetHotkeyID,
            ItemInstanceID = null,
        }
    });
    if (result.ResolveSuccess(out var response) && !string.IsNullOrEmpty(response.Data?.HotkeyID))
    {
        $"Hotkey triggered successfully!".Out(ConsoleColor.Green);
    }
    else
    {
        $"Failed to trigger a hotkey!".Out(ConsoleColor.Yellow);
    }
}
 */