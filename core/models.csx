using System;
using System.Collections.Generic;

public class Dataset
{
    public string Id { get; set; }

    public string Name { get; set; }

    public IList<object> Tables { get; set; }

    public string WebUrl { get; set; }
}

public class GatewayDataSource
{
    public string OdataContext { get; set; }

    public IList<Value> Value { get; set; }
}

public class PbixImport
{
    public DateTime CreatedDateTime { get; set; }

    public IList<Dataset> Datasets { get; set; }

    public string Id { get; set; }

    public string ImportState { get; set; }

    public string Name { get; set; }

    public IList<Report> Reports { get; set; }

    public DateTime UpdatedDateTime { get; set; }
}

public class Report
{
    public string EmbedUrl { get; set; }

    public string Id { get; set; }

    public string Name { get; set; }

    public string WebUrl { get; set; }
}

public enum RequestType
{
    Create,
    Import
}

public class ReqBody
{
    public string AppKey { get; set; }

    public string CollectionName { get; set; }

    public string DatasetName { get; set; }

    public byte[] FileBytes { get; set; }

    public string Password { get; set; }

    public string Username { get; set; }

    public string WorkspaceId { get; set; }
}

public class WorkspaceReport
{
    public string EmbedUrl { get; set; }

    public string Id { get; set; }

    public bool IsFromPbix { get; set; }

    public string Name { get; set; }

    public string WebUrl { get; set; }
}

public class WorkspaceReports
{
    public string OdataContext { get; set; }

    public IList<WorkspaceReport> Reports { get; set; }
}

public class Value
{
    public string ConnectionDetails { get; set; }

    public string DatasourceType { get; set; }

    public string GatewayId { get; set; }

    public string Id { get; set; }
}