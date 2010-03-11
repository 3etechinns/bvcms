<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<CMSWeb.Models.PagerModel2>" %>
<div class="pager">
    <span class="page-numbers desc">Goto page:</span>
    <% if (Model.Page > 1)
       { %>
    <a href='#' class="page-numbers" title='goto page <%=Model.Page-1 %>' onclick='return $.gotoPage(this,<%=Model.Page -1 %>)'>prev</a>
    <% }
       foreach (var p in Model.PageList())
       {
       if (p == 0)
       { %>
    <span class="page-numbers dots">&hellip;</span>
    <% }
       if (p > 0 && Model.Page != p)
       { %>
    <a href='#' class='page-numbers' title='goto page <%=p %>' onclick='return $.gotoPage(this,<%=p %>)'><%=p %></a>
    <% }
       if (Model.Page == p)
       { %>
    <span class="current page-numbers" title="goto page"><%=p %></span>
    <% }
       }
       if (Model.Page < Model.LastPage)
       { %>
    <a href='#' class="page-numbers" title='goto page <%=Model.Page+1 %>' onclick='return $.gotoPage(this,<%=Model.Page+1 %>)'>next</a>
    <% } %>
    <span class="page-numbers desc">Set # rows:</span>
    <%=Html.DropDownList("PageSize", Model.PageSizeList(), new { onchange = "$.setPageSize(this)" }) %>
</div>
<div style="clear:both"></div>
<%=Html.Hidden("Page")%>
<%=Html.Hidden("Sort")%>
<%=Html.Hidden("Direction")%>

