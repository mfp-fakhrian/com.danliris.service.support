﻿using com.danliris.support.lib.Helpers;
using com.danliris.support.lib.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace com.danliris.support.lib.Services
{
    public class MachineMutationService
    {
		SupportDbContext context;
		public MachineMutationService(SupportDbContext _context)
		{
			this.context = _context;
		}


		public IQueryable<FinishedGoodViewModel> GetMachineMutationReport(DateTime? dateFrom, DateTime? dateTo, int offset)
		{
			DateTime Datefrom = dateFrom == null ? new DateTime(1970, 1, 1) : (DateTime)dateFrom;
			DateTime Dateto = dateTo == null ? new DateTime(1970, 1, 1) : (DateTime)dateTo;
			string DateFrom = Datefrom.ToString("yyyy-MM-dd");
			string DateTo = Dateto.ToString("yyyy-MM-dd");
			List<FinishedGoodViewModel> machine = new List<FinishedGoodViewModel>();
			try
			{
				string connectionString = APIEndpoint.ConnectionString;
				using (SqlConnection conn =
					new SqlConnection(connectionString))
				{
					conn.Open();
					using (SqlCommand cmd = new SqlCommand("declare @TglLaporan datetime='"+DateFrom+"'"+
														   "declare @Tgl2 datetime = '" + DateTo + "'" +
															"select B.KodeBarang, B.NamaBarang, B.Sat,convert(float, B.SaldoAwal) as SaldoAwal,convert(float, B.Pemasukan) as Pemasukan,convert(float, B.Pengeluaran) as Pengeluaran,convert(float,B.Penyesuaian) as Penyesuaian,convert(float, (B.SaldoAwal + (B.Pemasukan - B.Pengeluaran))) as SaldoBuku,convert(float, B.StockOpname) as StockOpname, convert(float,B.Selisih) as Selisih, B.ActivateDate, B.CloseDate from " +
															"(select A.KodeBarang, A.NamaBarang, A.Sat,(A.SaldoAwal + (select isnull(SUM(TransactionAmount), 0) from dbo.MachineMutation where MachineID = A.KodeBarang and TransactionType = 'IN' and TransactionDate < @TglLaporan) - (select isnull(SUM(TransactionAmount), 0) from dbo.MachineMutation where MachineID = A.KodeBarang and TransactionType = 'OUT' and TransactionDate < @TglLaporan)) as SaldoAwal,	(select isnull(SUM(TransactionAmount), 0) from dbo.MachineMutation where MachineID = A.KodeBarang and TransactionType = 'IN' and TransactionDate>= @TglLaporan and TransactionDate<DATEADD(DAY,1,@Tgl2)) as Pemasukan, "+
															"(select isnull(SUM(TransactionAmount), 0) from dbo.MachineMutation where MachineID = A.KodeBarang and TransactionType = 'OUT' and TransactionDate>= @TglLaporan and TransactionDate<DATEADD(DAY,1,@Tgl2)) as Pengeluaran, "+
															"A.Penyesuaian, A.StockOpname, A.Selisih, A.ActivateDate, A.CloseDate from view_machinereport A where A.ActivateDate <= @Tgl2)B "+
															"where B.SaldoAwal > 0 or(B.SaldoAwal + (B.Pemasukan - B.Pengeluaran)) > 0 order by B.KodeBarang  ", conn))
					{
						SqlDataAdapter dataAdapter = new SqlDataAdapter(cmd);
						DataSet dSet = new DataSet();
						dataAdapter.Fill(dSet);
						foreach (DataRow data in dSet.Tables[0].Rows)
						{
							FinishedGoodViewModel view = new FinishedGoodViewModel
							{
								KodeBarang = data["KodeBarang"].ToString(),
								NamaBarang = data["NamaBarang"].ToString(),
								Pemasukan = (double)data["Pemasukan"],
								Pengeluaran = (double)data["Pengeluaran"],
								SaldoAwal = (double)data["SaldoAwal"],
								SaldoBuku = (double)data["SaldoBuku"],
								UnitQtyName = data["Sat"].ToString(),
								Penyesuaian = (double)data["Penyesuaian"],
								StockOpname = (double)data["StockOpname"],
								Selisih = (double)data["Selisih"]
							};
							machine.Add(view);
						}
					}
					conn.Close();
				}
			}
			catch (SqlException ex)
			{
				//Log exception
				//Display Error message
			}

			return machine.AsQueryable();
		}
	}
}