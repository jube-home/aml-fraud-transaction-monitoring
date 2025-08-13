using System;

namespace Jube.Data.Cache.Dto;

public class ExpiredTtlCounterEntryDto
{
    public DateTime ReferenceDate { get; set; }
    public string DataValue { get; set; }
    public int Value { get; set; }
}