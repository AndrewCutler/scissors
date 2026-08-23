using System;
using System.Data.Common;

public sealed record Clipping
{
    private Clipping(DateTimeOffset capturedAt, string text, int? id, Guid? temporaryId)
    {
        Text = text;
        Id = id;
        TemporaryId = temporaryId;
        CapturedAt = capturedAt;
    }

    public static Clipping FromPaste(DateTimeOffset capturedAt, string text)
    {
        return new Clipping(
            capturedAt: capturedAt,
            text: text,
            temporaryId: Guid.NewGuid(),
            id: null);
    }

    public static Clipping FromDTO(ClippingResponseDTO dto)
    {
        return new Clipping(
            capturedAt: dto.CapturedAt,
            id: dto.Id,
            text: dto.Text,
            temporaryId: null
        );
    }


    public int? Id { get; set; }
    public Guid? TemporaryId { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public string Text { get; }

    public string CapturedAtText => CapturedAt.ToString("HH:mm:ss");
    public bool ClientSideOnly => Id is null;

}