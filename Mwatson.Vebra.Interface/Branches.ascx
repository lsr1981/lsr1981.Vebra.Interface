<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Branches.ascx.cs" Inherits="MWatson.Vebra.Interface.Branches" %>
<style>
    .branch-table
    {
        border-collapse:collapse;
        margin-left:20px;
        margin-right:20px;
        margin-top:20px;
        margin-bottom:20px;
    }
    .branch-table,th, td
    {
        border: 1px solid #eee;
    }
    .branch-table td,
    .branch-table th 
    {
        padding:10px;
    }
    .branch-table td.center,
    .branch-table th.center
    {
        text-align:center;
        vertical-align:top;  
        padding-top:20px;
    }
    p.warning 
    {
        color:red;
    }
</style>
<p>Please use the checkboxes below to choose that branches that you want automatically get the properties data from.</p>
<asp:Panel ID="BranchPanel" runat="server"></asp:Panel>
<asp:HiddenField ID="StoredValue" runat="server" />
