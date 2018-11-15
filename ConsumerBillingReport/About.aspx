<%@ Page Title="About" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="ConsumerBillingReports.About" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="ajaxToolkit" %>
<%@ Register Assembly="CrystalDecisions.Web, Version=13.0.3500.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" Namespace="CrystalDecisions.Web" TagPrefix="CR" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>

    <asp:Label class="jumbotron" runat="server">
    <asp:Label id="lblStartDate" AssociatedControlId="ddlWeekOf" Text="Week of:" runat="server" >
        <asp:DropDownList ID="ddlWeekOf" runat="server"></asp:DropDownList>           
        <asp:Button ID="SubmitButton" runat="server" Text="Submit" OnClick="SubmitButton_OnClick"/>
    </asp:Label>
    <div>  
        <CR:CrystalReportViewer ID="CrystalReportViewer1" runat="server" AutoDataBind="true" ToolPanelView="None" HasToggleGroupTreeButton="False" HasToggleParameterPanelButton="False" />
    </div>
</asp:Content>
