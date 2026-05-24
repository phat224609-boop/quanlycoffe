using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using Newtonsoft.Json;
using quanlycoffe.Models;
using quanlycoffe.Models.View1Models;
using ClosedXML.Excel;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
namespace quanlycoffe.Controllers
{
    public class ThongKeController : Controller
    {
        BMSModel db = new BMSModel();

        public ActionResult DoanhThu(
            DateTime? tuNgay,
            DateTime? denNgay,
            string loaiLoc)
        {
            DateTime start;
            DateTime end;



            // Hôm nay
            if (loaiLoc == "homnay")
            {
                start = DateTime.Today;

                end = DateTime.Today
                    .AddDays(1)
                    .AddSeconds(-1);
            }

            // Hôm qua
            else if (loaiLoc == "homqua")
            {
                start = DateTime.Today.AddDays(-1);

                end = start
                    .AddDays(1)
                    .AddSeconds(-1);
            }

            // 7 ngày
            else if (loaiLoc == "7ngay")
            {
                start = DateTime.Today.AddDays(-6);

                end = DateTime.Today
                    .AddDays(1)
                    .AddSeconds(-1);
            }

            // Tùy chỉnh
            else
            {
                start = tuNgay ?? DateTime.Today;

                end = denNgay ?? DateTime.Today;

                end = end
                    .AddDays(1)
                    .AddSeconds(-1);
            }

            // =========================
            // DANH SÁCH HÓA ĐƠN
            // =========================

            var hoaDons = db.HoaDonBH
                .Where(x =>
                    x.NgayBan >= start &&
                    x.NgayBan <= end)
                .OrderByDescending(x => x.NgayBan)
                .ToList();

            // =========================
            // TOP SẢN PHẨM
            // =========================

            var topSanPham = db.CTHoaDonBH
                .Where(x =>
                    x.HoaDonBH.NgayBan >= start &&
                    x.HoaDonBH.NgayBan <= end)
                .GroupBy(x => new
                {
                    x.MaSP,
                    x.SanPham.TenSP
                })
                .Select(g => new TopSanPhamViewModel
                {
                    TenSanPham = g.Key.TenSP,

                    SoLuongBan = g.Sum(x => x.SoLuong),

                    DoanhThu = (double)g.Sum(x => x.ThanhTien)
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(5)
                .ToList();

            // =========================
            // SẢN PHẨM SẮP HẾT
            // =========================

            var sapHetHang = db.SanPham
                .Where(x => x.SoLuongTrongKho < 5)
                .OrderBy(x => x.SoLuongTrongKho)
                .ToList();

            // =========================
            // MODEL
            // =========================

            ThongKeViewModel model =
                new ThongKeViewModel();

            model.TuNgay = start;

            model.DenNgay = end;

            model.TongDoanhThu =
                (double)(hoaDons.Sum(x =>
                    (decimal?)x.TongThu) ?? 0);

            model.TongHoaDon =
                hoaDons.Count();

            model.HoaDons =
                hoaDons;

            model.TopSanPham =
                topSanPham;

            // =========================
            // VIEWBAG
            // =========================

            ViewBag.LoaiLoc = loaiLoc;

            ViewBag.DanhSachSapHetHang =
                sapHetHang;

            ViewBag.DanhSachBanChay =
                topSanPham;

            ViewBag.TuNgay_Raw =
                start.ToString("yyyy-MM-dd");

            ViewBag.DenNgay_Raw =
                end.ToString("yyyy-MM-dd");

            // =========================
            // BIỂU ĐỒ
            // =========================

            var chart = hoaDons
                .GroupBy(x => x.NgayBan.Date)
                .Select(g => new
                {
                    Ngay = g.Key,
                    TongTien = g.Sum(x => x.TongThu)
                })
                .OrderBy(x => x.Ngay)
                .ToList();

            var chartLabels = chart
                .Select(x => x.Ngay.ToString("dd/MM"))
                .ToList();

            var chartData = chart
                .Select(x => x.TongTien)
                .ToList();

            ViewBag.ChartLabels =
                JsonConvert.SerializeObject(chartLabels);

            ViewBag.ChartData =
                JsonConvert.SerializeObject(chartData);

            return View(model);
        }
        public ActionResult XuatExcel(DateTime? tuNgay, DateTime? denNgay)
        {
            DateTime start = tuNgay ?? DateTime.Today;
            DateTime end = denNgay ?? DateTime.Today;

            end = end.AddDays(1).AddSeconds(-1);

            var result = (from hd in db.HoaDonBH
                          join ct in db.CTHoaDonBH on hd.MaHD equals ct.MaHD
                          join sp in db.SanPham on ct.MaSP equals sp.MaSP
                          where hd.NgayBan >= start && hd.NgayBan <= end
                          select new
                          {
                              MaHD = hd.MaHD,
                              NgayBan = hd.NgayBan,
                              TenSanPham = sp.TenSP,
                              SoLuong = ct.SoLuong,
                              DonGia = ct.DonGia,
                              ThanhTien = ct.ThanhTien
                          }).ToList();

            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("BaoCao");

                ws.Cell(1, 1).Value = "Mã HD";
                ws.Cell(1, 2).Value = "Ngày Bán";
                ws.Cell(1, 3).Value = "Sản Phẩm";
                ws.Cell(1, 4).Value = "Số Lượng";
                ws.Cell(1, 5).Value = "Đơn Giá";
                ws.Cell(1, 6).Value = "Thành Tiền";

                int row = 2;

                foreach (var item in result)
                {
                    ws.Cell(row, 1).Value = item.MaHD;
                    ws.Cell(row, 2).Value = item.NgayBan.ToString("dd/MM/yyyy HH:mm");
                    ws.Cell(row, 3).Value = item.TenSanPham;
                    ws.Cell(row, 4).Value = item.SoLuong;
                    ws.Cell(row, 5).Value = item.DonGia;
                    ws.Cell(row, 6).Value = item.ThanhTien;
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);

                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "BaoCaoDoanhThu.xlsx"
                    );
                }
            }
        }
        public ActionResult XuatPDF(DateTime? tuNgay, DateTime? denNgay)
        {
            DateTime start = tuNgay ?? DateTime.Today;
            DateTime end = denNgay ?? DateTime.Today;

            end = end.AddDays(1).AddSeconds(-1);

            var data = (from hd in db.HoaDonBH
                        join ct in db.CTHoaDonBH on hd.MaHD equals ct.MaHD
                        join sp in db.SanPham on ct.MaSP equals sp.MaSP
                        where hd.NgayBan >= start && hd.NgayBan <= end
                        select new
                        {
                            hd.MaHD,
                            hd.NgayBan,
                            sp.TenSP,
                            ct.SoLuong,
                            ct.DonGia,
                            ThanhTien = ct.ThanhTien
                        }).ToList();

            MemoryStream stream = new MemoryStream();
            Document pdfDoc = new Document(PageSize.A4);

            PdfWriter.GetInstance(pdfDoc, stream).CloseStream = false;
            pdfDoc.Open();

            // ===== FONT TIẾNG VIỆT =====
            BaseFont bf = BaseFont.CreateFont(
                @"C:\Windows\Fonts\arial.ttf",
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED
            );

            Font font = new Font(bf, 11);
            Font fontBold = new Font(bf, 12, Font.BOLD);

            // ===== TITLE =====
            pdfDoc.Add(new Paragraph("BÁO CÁO DOANH THU CHI TIẾT\n\n", fontBold));

            PdfPTable table = new PdfPTable(5);
            table.WidthPercentage = 100;

            // ===== HEADER =====
            table.AddCell(new Phrase("Mã HD", fontBold));
            table.AddCell(new Phrase("Sản Phẩm", fontBold));
            table.AddCell(new Phrase("Số Lượng", fontBold));
            table.AddCell(new Phrase("Đơn Giá", fontBold));
            table.AddCell(new Phrase("Thành Tiền", fontBold));

            var group = data.GroupBy(x => x.MaHD);

            decimal tongTatCa = 0;

            // ===== DATA =====
            foreach (var hd in group)
            {
                foreach (var item in hd)
                {
                    table.AddCell(new Phrase(item.MaHD.ToString(), font));
                    table.AddCell(new Phrase(item.TenSP, font));

                    // SỐ LƯỢNG (SỐ NGUYÊN)
                    table.AddCell(new Phrase(
                        ((int)item.SoLuong).ToString(),
                        font
                    ));

                    table.AddCell(new Phrase(
                        string.Format("{0:N0}", item.DonGia),
                        font
                    ));

                    table.AddCell(new Phrase(
                        string.Format("{0:N0} VND", item.ThanhTien),
                        font
                    ));
                }

                decimal tongHD = hd.Sum(x => x.ThanhTien);
                tongTatCa += tongHD;

                PdfPCell cell = new PdfPCell(new Phrase(
                    $"TỔNG HÓA ĐƠN #{hd.Key}: {string.Format("{0:N0}", tongHD)} VND",
                    fontBold
                ));

                cell.Colspan = 5;
                cell.HorizontalAlignment = 2;
                cell.Padding = 5;

                table.AddCell(cell);
            }

            // ===== TOTAL =====
            PdfPCell total = new PdfPCell(new Phrase(
                $"TỔNG DOANH THU: {string.Format("{0:N0}", tongTatCa)} VND",
                fontBold
            ));

            total.Colspan = 5;
            total.HorizontalAlignment = 2;
            total.Padding = 8;

            table.AddCell(total);

            pdfDoc.Add(table);
            pdfDoc.Close();

            stream.Position = 0;

            return File(stream.ToArray(), "application/pdf", "BaoCaoDoanhThu.pdf");
        }
    }
    public class BaoCaoDoanhThuChiTietVM
    {
        public int MaHD { get; set; }
        public DateTime NgayBan { get; set; }

        public string TenSanPham { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }

        public decimal ThanhTien { get; set; }
    }
}