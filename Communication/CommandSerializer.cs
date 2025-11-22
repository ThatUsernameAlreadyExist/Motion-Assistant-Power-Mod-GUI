using BidirectionalPipe.ActorModel;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

public class ZeroCopyCommandSerializer
{
    // Command type identifiers
    private const byte CMD_STRING = 1;
    private const byte CMD_INT = 2;
    private const byte CMD_UINT = 3;
    private const byte CMD_FLOAT = 4;
    private const byte CMD_CONTAINER = 5;
    private const byte CMD_STRING_LIST = 6;
    private const byte CMD_INT_LIST = 7;
    private const byte CMD_LIST_GENERIC = 8;

    // Dictionary value type identifiers
    private const byte TYPE_NULL = 0;
    private const byte TYPE_STRING = 1;
    private const byte TYPE_INT = 2;
    private const byte TYPE_UINT = 3;
    private const byte TYPE_FLOAT = 4;
    private const byte TYPE_BOOL = 5;
    private const byte TYPE_LONG = 6;
    private const byte TYPE_DOUBLE = 7;
    private const byte TYPE_STRING_LIST = 10;
    private const byte TYPE_INT_LIST = 11;
    private const byte TYPE_DATETIME = 12;

    [ThreadStatic]
    private static MemoryStream t_stream;

    [ThreadStatic]
    private static BinaryWriter t_writer;

    public byte[] SerializeCommand(ActorPipe.CommandBase command)
    {
        if (t_stream == null)
        {
            t_stream = new MemoryStream(512);
            t_writer = new BinaryWriter(t_stream);
        }
        else
        {
            t_stream.SetLength(0);
        }

        try
        {
            WriteCommand(t_writer, command);
            t_writer.Flush();
            return t_stream.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Serialization error: {ex.Message}");
            throw new InvalidOperationException($"Failed to serialize command: {ex.Message}", ex);
        }
    }

    public ArraySegment<byte> SerializeCommandPooled(ActorPipe.CommandBase command, out byte[] rentedBuffer)
    {
        rentedBuffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            using (var stream = new MemoryStream(rentedBuffer))
            using (var writer = new BinaryWriter(stream))
            {
                WriteCommand(writer, command);
                writer.Flush();

                int length = (int)stream.Position;
                return new ArraySegment<byte>(rentedBuffer, 0, length);
            }
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
            throw;
        }
    }

    public void ReturnBuffer(byte[] buffer)
    {
        if (buffer != null)
            ArrayPool<byte>.Shared.Return(buffer);
    }

    public ActorPipe.CommandBase DeserializeCommand(byte[] data)
    {
        if (data == null || data.Length == 0)
            return null;

        try
        {
            using (var stream = new MemoryStream(data, false))
            using (var reader = new BinaryReader(stream))
            {
                return ReadCommand(reader);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Deserialization error: {ex.Message}");
            return null;
        }
    }

    public ActorPipe.CommandBase DeserializeCommand(ArraySegment<byte> data)
    {
        if (data.Count == 0)
            return null;

        try
        {
            using (var stream = new MemoryStream(data.Array, data.Offset, data.Count, false))
            using (var reader = new BinaryReader(stream))
            {
                return ReadCommand(reader);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Deserialization error: {ex.Message}");
            return null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteCommand(BinaryWriter writer, ActorPipe.CommandBase command)
    {
        byte cmdType;
        switch (command)
        {
            case ActorPipe.StringCommand _:
                cmdType = CMD_STRING;
                break;
            case ActorPipe.IntCommand _:
                cmdType = CMD_INT;
                break;
            case ActorPipe.UintCommand _:
                cmdType = CMD_UINT;
                break;
            case ActorPipe.FloatCommand _:
                cmdType = CMD_FLOAT;
                break;
            case ActorPipe.ContainerCommand _:
                cmdType = CMD_CONTAINER;
                break;
            case ActorPipe.StringListCommand _:
                cmdType = CMD_STRING_LIST;
                break;
            case ActorPipe.IntListCommand _:
                cmdType = CMD_INT_LIST;
                break;
            case ActorPipe.ListCommand<string> _:
                cmdType = CMD_LIST_GENERIC;
                break;
            default:
                throw new NotSupportedException($"Command type {command.GetType().Name} not supported");
        }

        writer.Write(cmdType);
        WriteBaseProperties(writer, command);

        switch (command)
        {
            case ActorPipe.StringCommand cmd:
                writer.Write(cmd.Data ?? string.Empty);
                break;
            case ActorPipe.IntCommand cmd:
                writer.Write(cmd.Data);
                break;
            case ActorPipe.UintCommand cmd:
                writer.Write(cmd.Data);
                break;
            case ActorPipe.FloatCommand cmd:
                writer.Write(cmd.Data);
                break;
            case ActorPipe.ContainerCommand cmd:
                WriteContainerCommand(writer, cmd);
                break;
            case ActorPipe.StringListCommand cmd:
                WriteStringList(writer, cmd.Data);
                break;
            case ActorPipe.IntListCommand cmd:
                WriteIntList(writer, cmd.Data);
                break;
            case ActorPipe.ListCommand<string> cmd:
                WriteStringList(writer, cmd.Data);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteBaseProperties(BinaryWriter writer, ActorPipe.CommandBase command)
    {
        writer.Write(command.CommandId ?? string.Empty);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ActorPipe.CommandBase ReadCommand(BinaryReader reader)
    {
        byte cmdType = reader.ReadByte();

        string commandId = reader.ReadString();

        ActorPipe.CommandBase result;

        switch (cmdType)
        {
            case CMD_STRING:
                {
                    string data = reader.ReadString();
                    result = new ActorPipe.StringCommand(data, commandId);
                    break;
                }
            case CMD_INT:
                {
                    int data = reader.ReadInt32();
                    result = new ActorPipe.IntCommand(data, commandId);
                    break;
                }
            case CMD_UINT:
                {
                    uint data = reader.ReadUInt32();
                    result = new ActorPipe.UintCommand(data, commandId);
                    break;
                }
            case CMD_FLOAT:
                {
                    float data = reader.ReadSingle();
                    result = new ActorPipe.FloatCommand(data, commandId);
                    break;
                }
            case CMD_CONTAINER:
                {
                    result = ReadContainerCommand(reader, commandId);
                    break;
                }
            case CMD_STRING_LIST:
                {
                    var data = ReadStringList(reader);
                    result = new ActorPipe.StringListCommand(data, commandId);
                    break;
                }
            case CMD_INT_LIST:
                {
                    var data = ReadIntList(reader);
                    result = new ActorPipe.IntListCommand(data, commandId);
                    break;
                }
            case CMD_LIST_GENERIC:
                {
                    var data = ReadStringList(reader);
                    result = new ActorPipe.ListCommand<string>(data, commandId);
                    break;
                }
            default:
                return null;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteContainerCommand(BinaryWriter writer, ActorPipe.ContainerCommand cmd)
    {
        WriteDictionary(writer, cmd.Data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ActorPipe.ContainerCommand ReadContainerCommand(BinaryReader reader, string commandId)
    {
        var data = ReadDictionary(reader);
        return new ActorPipe.ContainerCommand(data, commandId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteDictionary(BinaryWriter writer, Dictionary<string, object> dict)
    {
        if (dict == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(dict.Count);

        foreach (var kvp in dict)
        {
            writer.Write(kvp.Key ?? string.Empty);
            WriteObject(writer, kvp.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Dictionary<string, object> ReadDictionary(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count == 0) return null;

        var dict = new Dictionary<string, object>(count);

        for (int i = 0; i < count; i++)
        {
            string key = reader.ReadString();
            object value = ReadObject(reader);
            dict[key] = value;
        }

        return dict;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteObject(BinaryWriter writer, object obj)
    {
        if (obj == null)
        {
            writer.Write(TYPE_NULL);
            return;
        }

        switch (obj)
        {
            case string str:
                writer.Write(TYPE_STRING);
                writer.Write(str);
                break;
            case int i:
                writer.Write(TYPE_INT);
                writer.Write(i);
                break;
            case uint ui:
                writer.Write(TYPE_UINT);
                writer.Write(ui);
                break;
            case float f:
                writer.Write(TYPE_FLOAT);
                writer.Write(f);
                break;
            case bool b:
                writer.Write(TYPE_BOOL);
                writer.Write(b);
                break;
            case long l:
                writer.Write(TYPE_LONG);
                writer.Write(l);
                break;
            case double d:
                writer.Write(TYPE_DOUBLE);
                writer.Write(d);
                break;
            case DateTime dt:
                writer.Write(TYPE_DATETIME);
                writer.Write(dt.ToBinary());
                break;
            case List<string> strList:
                writer.Write(TYPE_STRING_LIST);
                WriteStringList(writer, strList);
                break;
            case List<int> intList:
                writer.Write(TYPE_INT_LIST);
                WriteIntList(writer, intList);
                break;
            default:
                // Fallback for unsupported types
                writer.Write(TYPE_STRING);
                writer.Write(obj.ToString());
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object ReadObject(BinaryReader reader)
    {
        byte typeId = reader.ReadByte();

        switch (typeId)
        {
            case TYPE_NULL:
                return null;
            case TYPE_STRING:
                return reader.ReadString();
            case TYPE_INT:
                return reader.ReadInt32();
            case TYPE_UINT:
                return reader.ReadUInt32();
            case TYPE_FLOAT:
                return reader.ReadSingle();
            case TYPE_BOOL:
                return reader.ReadBoolean();
            case TYPE_LONG:
                return reader.ReadInt64();
            case TYPE_DOUBLE:
                return reader.ReadDouble();
            case TYPE_DATETIME:
                return DateTime.FromBinary(reader.ReadInt64());
            case TYPE_STRING_LIST:
                return ReadStringList(reader);
            case TYPE_INT_LIST:
                return ReadIntList(reader);
            default:
                return null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteStringList(BinaryWriter writer, List<string> list)
    {
        int count = list?.Count ?? 0;
        writer.Write(count);

        if (count > 0)
        {
            foreach (var item in list)
                writer.Write(item ?? string.Empty);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<string> ReadStringList(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count == 0) return null;

        var list = new List<string>(count);
        for (int i = 0; i < count; i++)
            list.Add(reader.ReadString());

        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteIntList(BinaryWriter writer, List<int> list)
    {
        int count = list?.Count ?? 0;
        writer.Write(count);

        if (count > 0)
        {
            foreach (var item in list)
                writer.Write(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<int> ReadIntList(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count == 0) return null;

        var list = new List<int>(count);
        for (int i = 0; i < count; i++)
            list.Add(reader.ReadInt32());

        return list;
    }
}