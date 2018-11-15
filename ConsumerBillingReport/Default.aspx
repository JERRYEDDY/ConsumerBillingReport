<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ConsumerBillingReports._Default" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="ajaxToolkit" %>
<%@ Register Assembly="CrystalDecisions.Web, Version=13.0.3500.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" Namespace="CrystalDecisions.Web" TagPrefix="CR" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <script src='<%=ResolveUrl("~/crystalreportviewers13/js/crviewer/crv.js")%>' type="text/javascript"></script>

    <asp:Label class="jumbotron" runat="server">
        <asp:Label id="lblStartDate" AssociatedControlId="ddlWeekOf" Text="Week of:" runat="server" >
        <asp:DropDownList ID="ddlWeekOf" runat="server"></asp:DropDownList>           

<%--            <asp:TextBox ID="txtStartDate" runat="server"></asp:TextBox>  
            <ajaxToolkit:CalendarExtender ID="calStartDate" runat="server" TargetControlID="txtStartDate" Format="MM/dd/yyyy"></ajaxToolkit:CalendarExtender>  --%>
        </asp:Label>  
<%--        <asp:Label id="lblEndDate" AssociatedControlId="txtEndDate" Text=" End Date:" runat="server" > 
            <asp:TextBox ID="txtEndDate" runat="server"></asp:TextBox>  
            <ajaxToolkit:CalendarExtender ID="calEndDate" runat="server" TargetControlID="txtEndDate" Format="MM/dd/yyyy"></ajaxToolkit:CalendarExtender>  
        </asp:Label>  --%>    

        <asp:Button ID="SubmitButton" runat="server" Text="Submit" OnClick="SubmitButton_OnClick"/>
    </asp:Label>
    <div>  
        <CR:CrystalReportViewer ID="CrystalReportViewer1" runat="server" AutoDataBind="true" ToolPanelView="None" HasToggleGroupTreeButton="False" HasToggleParameterPanelButton="False" />
    </div>

</asp:Content>
