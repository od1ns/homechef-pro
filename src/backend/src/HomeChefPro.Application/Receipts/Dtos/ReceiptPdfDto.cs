namespace HomeChefPro.Application.Receipts.Dtos;

public sealed record ReceiptPdfDto(byte[] Pdf, string FileName, string ContentType = "application/pdf");
