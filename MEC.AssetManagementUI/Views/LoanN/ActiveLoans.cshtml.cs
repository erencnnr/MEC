using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Threading;
using System.Xml.Linq;

@model MEC.AssetManagementUI.Models.LoanModel.ActiveLoanListViewModel

@{
    ViewData["Title"] = "Aktif Zimmetler";
}

< div class= "card shadow mb-4" >
    < div class= "card-header py-3 d-flex justify-content-between align-items-center" >
        < h6 class= "m-0 font-weight-bold text-primary" > Aktif Zimmet Listesi</h6>
    </div>
    <div class= "card-body" >


        < form asp - action = "ActiveLoans" method = "get" class= "mb-4" >
            < div class= "input-group" >
                < input type = "text" name = "searchTerm" class= "form-control" placeholder = "Personel veya Eþya ara..." value = "@Model.SearchTerm" >
                < button class= "btn btn-primary" type = "submit" >
                    < i class= "bi bi-search" ></ i > Ara
                </ button >
                @if(!string.IsNullOrEmpty(Model.SearchTerm))
                {
                    < a asp - action = "ActiveLoans" class= "btn btn-secondary" > Temizle </ a >
                }
            </ div >
        </ form >

        < div class= "table-responsive" >
            < table class= "table table-bordered table-hover" width = "100%" cellspacing = "0" >
                < thead class= "table-light" >
                    < tr >
                        < th > Eþya Adý </ th >
                        < th > Zimmetlenen Personel </ th >
                        < th > Veriliþ Tarihi </ th >
                        < th > Notlar </ th >
                        < th > Ýþlemler </ th >
                    </ tr >
                </ thead >
                < tbody >
                    @if(Model.ActiveLoans.Any())
                    {
    foreach (var item in Model.ActiveLoans)
    {
                            < tr >
                                < td >
                                    < span class= "fw-bold" > @item.Asset?.Name </ span >
                                    < br />
                                    < small class= "text-muted" > Seri No: @item.Asset?.SerialNumber </ small >
                                </ td >
                                < td > @item.AssignedTo?.FirstName @item.AssignedTo?.LastName </ td >
                                < td > @item.LoanDate.ToShortDateString() </ td >
                                < td > @item.Notes </ td >
                                < td >
                                    < form asp - action = "ReturnAsset" method = "post" class= "d-inline" onsubmit = "return confirm('Bu eþyayý iade almak istediðinize emin misiniz?');" >
                                        < input type = "hidden" name = "id" value = "@item.Id" />
                                        < button type = "submit" class= "btn btn-sm btn-warning text-white" >
                                            < i class= "bi bi-box-arrow-in-left" ></ i > Ýade Al
                                        </ button >
                                    </ form >
                                </ td >
                            </ tr >
                        }
                    }
                    else
{
                        < tr >
                            < td colspan = "5" class= "text-center" > Aktif zimmet bulunamadý.</ td >
                        </ tr >
                    }
                </ tbody >
            </ table >
        </ div >
    </ div >
</ div >