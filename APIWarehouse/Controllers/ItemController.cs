using APIWarehouse.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using DocumentFormat.OpenXml;
using WordDoc = DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using ClosedXML.Excel;

namespace APIWarehouse.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly WarehouseContext _context;

        public ProductsController(WarehouseContext context)
        {
            _context = context;
        }

        // Получить все товары
        [HttpGet]
        public async Task<ActionResult<List<Warehouse>>> GetProducts()
        {
            try
            {
                var products = await _context.Warehouses.ToListAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении данных: {ex.Message}");
            }
        }

        // Добавить новый товар
        [HttpPost]
        public async Task<ActionResult<Warehouse>> AddProduct([FromBody] Warehouse product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.Name))
                return BadRequest("Некорректные данные продукта.");

            try
            {
                product.WarehouseId = 0; // база сама присвоит
                _context.Warehouses.Add(product);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProducts), new { id = product.WarehouseId }, product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при добавлении продукта: {ex.Message}");
            }
        }

        // Удалить товар по Id
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Warehouses.FindAsync(id);
                if (product == null) return NotFound("Товар не найден.");

                _context.Warehouses.Remove(product);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при удалении продукта: {ex.Message}");
            }
        }
        
        [HttpGet("report/pdf")]
        public async Task<IActionResult> GetPdfReport()
        {
            var products = await _context.Warehouses.ToListAsync();

            using var stream = new MemoryStream();

            using (var writer = new iText.Kernel.Pdf.PdfWriter(stream))
            using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
            using (var document = new iText.Layout.Document(pdf))
            {
                document.Add(new iText.Layout.Element.Paragraph("Отчёт об остатках товаров на складе"));

                foreach (var p in products)
                {
                    document.Add(new iText.Layout.Element.Paragraph($"ID: {p.WarehouseId}"));
                    document.Add(new iText.Layout.Element.Paragraph($"Название: {p.Name}"));
                    document.Add(new iText.Layout.Element.Paragraph($"Количество: {p.Quantity}"));
                }
            }

            return File(
                stream.ToArray(),
                "application/pdf",
                "Report.pdf");
        }

        // Создать Word-отчёт
        [HttpGet("report/word")]
        public async Task<IActionResult> GetWordReport()
        {
            var products = await _context.Warehouses.ToListAsync();

            using var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(
                       stream,
                       WordprocessingDocumentType.Document,
                       true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new WordDoc.Document();
                var body = new WordDoc.Body();

                // Заголовок
                body.Append(new WordDoc.Paragraph(
                    new WordDoc.Run(
                        new WordDoc.Text("Отчёт об остатках товаров на складе"))));

                // Данные по товарам
                foreach (var p in products)
                {
                    body.Append(new WordDoc.Paragraph(
                        new WordDoc.Run(
                            new WordDoc.Text($"ID: {p.WarehouseId}"))));

                    body.Append(new WordDoc.Paragraph(
                        new WordDoc.Run(
                            new WordDoc.Text($"Название: {p.Name}"))));

                    body.Append(new WordDoc.Paragraph(
                        new WordDoc.Run(
                            new WordDoc.Text($"Количество: {p.Quantity}"))));
                }

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "Report.docx");
        }
        
        [HttpGet("report/excel")]
        public async Task<IActionResult> GetExcelReport()
        {
            var products = await _context.Warehouses.ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Склад");

            // Заголовки
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Название";
            worksheet.Cell(1, 3).Value = "Количество";
            worksheet.Range(1, 1, 1, 3).Style.Font.Bold = true;

            // Данные
            int row = 2;
            foreach (var p in products)
            {
                worksheet.Cell(row, 1).Value = p.WarehouseId;
                worksheet.Cell(row, 2).Value = p.Name;
                worksheet.Cell(row, 3).Value = p.Quantity;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Report.xlsx");
        }
    }
}
