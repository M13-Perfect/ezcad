namespace EngravingStation.Core.Exceptions;

public class EngravingStationException : Exception
{
    public EngravingStationException(string message) : base(message) { }
    public EngravingStationException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class BatchStateException : EngravingStationException
{
    public BatchStateException(string message) : base(message) { }
}

public sealed class CadAdapterException : EngravingStationException
{
    public CadAdapterException(string message) : base(message) { }
    public CadAdapterException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class RepositoryException : EngravingStationException
{
    public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class CsvImportException : EngravingStationException
{
    public CsvImportException(string message) : base(message) { }
    public CsvImportException(string message, Exception innerException) : base(message, innerException) { }
}
