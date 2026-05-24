using System;
using System.Collections.Generic;

namespace quanlycoffe.Models.View1Models
{
    public class ThongKeViewModel
    {
        public DateTime TuNgay { get; set; }

        public DateTime DenNgay { get; set; }

        public double TongDoanhThu { get; set; }

        public int TongHoaDon { get; set; }

        public List<HoaDonBH> HoaDons { get; set; }

        public List<TopSanPhamViewModel> TopSanPham { get; set; }
    }

    public class TopSanPhamViewModel
    {
        public string TenSanPham { get; set; }

        public decimal SoLuongBan { get; set; }

        public double DoanhThu { get; set; }
    }
}