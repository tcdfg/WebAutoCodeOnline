﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinGenerateCodeDB.Cache;

namespace WinGenerateCodeDB.Code
{
    public class AspxCsHelper_Bootstrap
    {
        public static string CreateASPXCS()
        {
            StringBuilder aspxcsContent = new StringBuilder();
            aspxcsContent.Append(CreateCSHead());
            aspxcsContent.Append(CreatePageLoad());
            aspxcsContent.Append(CreateLoadData());
            aspxcsContent.Append(CreateAddData());
            aspxcsContent.Append(CreateEditData());
            aspxcsContent.Append(CreateBatEditData());
            aspxcsContent.Append(CreateDeleteData());
            aspxcsContent.Append(CreateDownAndDownAll());
            aspxcsContent.Append(CreateBottom());

            return aspxcsContent.ToString();
        }

        private static string CreateCSHead()
        {
            return string.Format(@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Script.Serialization;
using System.Text;

namespace {0}
{{
    public partial class {1} : System.Web.UI.Page
    {{", PageCache.NameSpaceStr, PageCache.TableName_UI);
        }

        private static string CreatePageLoad()
        {
            string template = @"
        protected void Page_Load(object sender, EventArgs e)
        {{
            if (Request.HttpMethod.ToUpper() == ""POST"")
            {{
                string type = Request.QueryString[""type""];
                switch (type)
                {{{0}    
                }}

                Response.Flush();
                Response.End();
            }}{1}
        }}
";
            StringBuilder content = new StringBuilder();
            content.Append(@"
                    case ""loaddata"":
                        LoadData();
                        break;");
            if (PageCache.GetCmd("添加") != null)
            {
                content.Append(@"
                    case ""add"":
                        AddData();
                        break;");
            }

            if (PageCache.GetCmd("编辑") != null)
            {
                content.Append(@"
                    case ""edit"":
                        EditData();
                        break;");
            }

            if (PageCache.GetCmd("批量编辑") != null)
            {
                content.Append(@"
                    case ""batedit"":
                        BatEditData();
                        break;");
            }

            if (PageCache.GetCmd("删除") != null)
            {
                content.Append(@"
                    case ""delete"":
                        DeleteData();
                        break;");
            }

            StringBuilder elseContent = new StringBuilder();
            if (PageCache.GetCmd("导出全部") != null || PageCache.GetCmd("导出选中") != null)
            {
                elseContent.AppendFormat(@"
            else{{
                string type = Request.QueryString[""type""];
                switch (type)
                {{
                    {0}
                    {1}
                }}
            }}", PageCache.GetCmd("导出选中") != null ? "case \"down\":\r\n\t\t\t\tDown();\r\n\t\t\t\tbreak;" : "",
               PageCache.GetCmd("导出全部") != null ? "case \"downall\":\r\n\t\t\t\tDownAll();\r\n\t\t\t\tbreak;" : "");
            }

            return string.Format(template, content.ToString(), elseContent.ToString());
        }

        private static string CreateLoadData()
        {
            string template = @"
        private void LoadData()
        {{
            int page = Convert.ToInt32(Request.Form[""page""]);
            int pageSize = Convert.ToInt32(Request.Form[""pageSize""]);
{0}
            {1} dal = new {1}();
            var list = dal.QueryList({2}page, pageSize);
{4}
            int itemCount = dal.QueryListCount({3});
            int pageCount = (int)Math.Ceiling((double)itemCount / (double)pageSize);
			JavaScriptSerializer js = new JavaScriptSerializer();
            var str = js.Serialize(new {{ PageCount = pageCount, ItemCount = itemCount, Data = list }});
            Response.Write(str);
        }}
";
            StringBuilder searchContent = new StringBuilder();
            StringBuilder searchStrContent = new StringBuilder();
            var showModel = PageCache.GetCmd("主显示");
            if (showModel != null)
            {
                foreach (var item in showModel.AttrList)
                {
                    string attribute = item.AttrName;
                    if (item.DbType.ToLower() == "datetime" ||
                        item.DbType.ToLower() == "date" ||
                        item.DbType.ToLower() == "int" ||
                        item.DbType.ToLower() == "bigint" ||
                        item.DbType.ToLower() == "tinyint")
                    {
                        searchContent.AppendFormat("\t\t\tstring {0}Str = HttpUtility.UrlDecode(Request[\"txtSearch{0}\"]);\r\n", attribute);
                        if (item.DbType.ToLower() == "datetime" || item.DbType.ToLower() == "date")
                        {
                            searchContent.AppendFormat("\t\t\tDateTime {0} = DateTime.MinValue;\r\n", attribute);
                            searchContent.AppendFormat("\t\t\tDateTime.TryParse({0}Str, out {0});\r\n", attribute);
                        }
                        else
                        {
                            searchContent.AppendFormat("\t\t\tint {0} = 0;\r\n", attribute);
                            searchContent.AppendFormat("\t\t\tif(!int.TryParse({0}Str, out {0}))\r\n", attribute);
                            searchContent.Append("\t\t\t{\r\n");
                            searchContent.AppendFormat("\t\t\t\t{0} = -1;\r\n", attribute);
                            searchContent.Append("\t\t\t}\r\n");
                        }
                    }
                    else
                    {
                        searchContent.AppendFormat("\t\t\tstring {0} = HttpUtility.UrlDecode(Request[\"txtSearch{0}\"]);\r\n", attribute);
                    }

                    searchStrContent.AppendFormat(" {0},", attribute);
                }

                StringBuilder encodeContent = new StringBuilder();
                foreach (var item in showModel.AttrList)
                {
                    if (item.DbType == "varchar")
                    {
                        string attribute = item.ColName;

                        encodeContent.AppendLine(string.Format("\t\t\tlist.ForEach(p => p.{0} = HttpUtility.HtmlEncode(p.{0}));", attribute));
                    }
                }

                // 移除最后的逗号
                string searchStrCount = searchStrContent.Length > 0 ? searchStrContent.ToString().Substring(0, searchStrContent.Length - 1) : searchStrContent.ToString();
                return string.Format(template, searchContent.ToString(), PageCache.TableName_DAL, searchStrContent.ToString(), searchStrCount, encodeContent.ToString());
            }
            else
            {
                return string.Empty;
            }
        }

        private static string CreateAddData()
        {
            StringBuilder addContent = new StringBuilder();
            StringBuilder createModel = new StringBuilder();
            var addModel = PageCache.GetCmd("添加");
            if (addModel != null)
            {
                foreach (var item in addModel.AttrList)
                {
                    string attribute = item.AttrName;
                    string eval_attribute = item.ColName;
                    if (item.DbType.ToLower() == "datetime" ||
                        item.DbType.ToLower() == "date" ||
                        item.DbType.ToLower() == "int" ||
                        item.DbType.ToLower() == "tinyint")
                    {
                        addContent.AppendFormat("\t\t\tstring {0}Str = HttpUtility.UrlDecode(Request[\"txtAdd{0}\"]);\r\n", attribute);
                        if (item.DbType.ToLower() == "datetime" || item.DbType.ToLower() == "date")
                        {
                            addContent.AppendFormat("\t\t\tDateTime {0} = DateTime.MinValue;\r\n", attribute);
                            addContent.AppendFormat("\t\t\tDateTime.TryParse({0}Str, out {0});\r\n", attribute);
                        }
                        else
                        {
                            addContent.AppendFormat("\t\t\tint {0} = 0;;\r\n", attribute);
                            addContent.AppendFormat("\t\t\tint.TryParse({0}Str, out {0});\r\n", attribute);
                        }

                        createModel.AppendFormat("\t\t\tmodel.{0} = {1};\r\n", eval_attribute, ExtendMethod.ToStringToType(attribute, item.DbType));
                    }
                    else
                    {
                        addContent.AppendFormat("\t\t\tstring {0} = HttpUtility.UrlDecode(Request[\"txtAdd{1}\"]);\r\n", attribute, attribute);
                        createModel.AppendFormat("\t\t\tmodel.{0} = {1};\r\n", eval_attribute, ExtendMethod.ToStringToType(attribute, item.DbType));
                    }
                }

                string template = @"
        private void AddData()
        {{
{0}
            {1} model = new {1}();
{2}
            {3} dal = new {3}();
            dal.Add{4}(model);

            Response.Write(""0"");
        }}
";

                return string.Format(template, addContent.ToString(), PageCache.TableName_Model, createModel.ToString(), PageCache.TableName_DAL, PageCache.TableName);
            }
            else
            {
                return string.Empty;
            }
        }

        private static string CreateEditData()
        {
            StringBuilder editContent = new StringBuilder();
            StringBuilder createModel = new StringBuilder();
            editContent.AppendFormat("\t\t\tstring {0} = HttpUtility.UrlDecode(Request[\"txtEdit{0}\"]);\r\n", PageCache.KeyId);
            createModel.AppendFormat("\t\t\tmodel.{0} = {1};\r\n", PageCache.KeyId, ExtendMethod.ToStringToType(PageCache.KeyId, PageCache.KeyId_DbType));
            var editModel = PageCache.GetCmd("编辑");
            if (editModel != null)
            {
                foreach (var item in editModel.AttrList)
                {
                    string attribute = item.AttrName;
                    string eval_attribute = item.ColName;
                    if (item.DbType.ToLower() == "datetime" ||
                        item.DbType.ToLower() == "date" ||
                        item.DbType.ToLower() == "int" ||
                        item.DbType.ToLower() == "tinyint")
                    {
                        editContent.AppendFormat("\t\t\tstring {0}Str = HttpUtility.UrlDecode(Request[\"txtEdit{0}\"]);\r\n", attribute);
                        if (item.DbType.ToLower() == "datetime" || item.DbType.ToLower() == "date")
                        {
                            editContent.AppendFormat("\t\t\tDateTime {0} = DateTime.MinValue;\r\n", attribute);
                            editContent.AppendFormat("\t\t\tDateTime.TryParse({0}Str, out {0});\r\n", attribute);
                        }
                        else
                        {
                            editContent.AppendFormat("\t\t\tint {0} = 0;\r\n", attribute);
                            editContent.AppendFormat("\t\t\tint.TryParse({0}Str, out {0});\r\n", attribute);
                        }

                        createModel.AppendFormat("\t\t\tmodel.{0} = {1};\r\n", eval_attribute, ExtendMethod.ToStringToType(attribute, item.DbType));
                    }
                    else
                    {
                        editContent.AppendFormat("\t\t\tstring {0} = HttpUtility.UrlDecode(Request[\"txtEdit{1}\"]);\r\n", attribute, attribute);
                        createModel.AppendFormat("\t\t\tmodel.{0} = {1};\r\n", eval_attribute, ExtendMethod.ToStringToType(eval_attribute, item.DbType));
                    }
                }

                string template = @"
        private void EditData()
        {{
{0}
            {1} model = new {1}();
{2}
            {3} dal = new {3}();
            dal.Update{4}(model);

            Response.Write(""0"");
        }}
";

                return string.Format(template, editContent.ToString(), PageCache.TableName_Model, createModel.ToString(), PageCache.TableName_DAL, PageCache.TableName);
            }
            else
            {
                return string.Empty;
            }
        }

        private static string CreateBatEditData()
        {
            StringBuilder batEditContent = new StringBuilder();
            StringBuilder createModel = new StringBuilder();
            batEditContent.AppendFormat("\t\t\tstring {0} = HttpUtility.UrlDecode(Request[\"txtBatEdit{0}\"]);\r\n", PageCache.KeyId);
            batEditContent.AppendFormat(@"           List<string> idList = {0}.Split(new char[]{{','}}, StringSplitOptions.RemoveEmptyEntries).ToList();{1}", PageCache.KeyId, Environment.NewLine);
            var batEditModel = PageCache.GetCmd("批量编辑");
            if (batEditModel != null)
            {
                foreach (var item in batEditModel.AttrList)
                {
                    string attribute = item.AttrName;
                    string eval_attribute = item.ColName;
                    if (item.DbType.ToLower() == "datetime" ||
                        item.DbType.ToLower() == "date" ||
                        item.DbType.ToLower() == "int" ||
                        item.DbType.ToLower() == "tinyint")
                    {
                        batEditContent.AppendFormat("\t\t\tstring {0}Str = HttpUtility.UrlDecode(Request[\"txtBatEdit{0}\"]);\r\n", attribute);
                        if (item.DbType.ToLower() == "datetime" || item.DbType.ToLower() == "date")
                        {
                            batEditContent.AppendFormat("\t\t\tDateTime {0} = DateTime.MinValue;\r\n", attribute);
                            batEditContent.AppendFormat("\t\t\tDateTime.TryParse({0}Str, out {0});\r\n", attribute);
                        }
                        else
                        {
                            batEditContent.AppendFormat("\t\t\tint {0} = 0;;\r\n", attribute);
                            batEditContent.AppendFormat("\t\t\tint.TryParse({0}Str, out {0});\r\n", attribute);
                        }

                        createModel.AppendFormat("\t\t\tmodel.{0} = {1};\r\n", eval_attribute, ExtendMethod.ToStringToType(eval_attribute, item.DbType));
                    }
                    else
                    {
                        batEditContent.AppendFormat("\t\t\tstring {0} = HttpUtility.UrlDecode(Request[\"txtBatEdit{0}\"]);\r\n", attribute);
                        createModel.AppendFormat("\t\t\tmodel.{0} = {1};\r\n", eval_attribute, ExtendMethod.ToStringToType(eval_attribute, item.DbType));
                    }
                }

                string template = @"
        private void BatEditData()
        {{
{0}
            {1} model = new {1}();
{2}
            {3} dal = new {3}();
            dal.BatUpdate{4}(idList, model);

            Response.Write(""0"");
        }}
";

                return string.Format(template, batEditContent.ToString(), PageCache.TableName_Model, createModel.ToString(), PageCache.TableName_DAL, PageCache.TableName);
            }
            else
            {
                return string.Empty;
            }
        }

        private static string CreateDeleteData()
        {
            var deleteModel = PageCache.GetCmd("删除");
            if (deleteModel != null)
            {
                StringBuilder batContent = new StringBuilder();
                batContent.Append("            string ids = HttpUtility.UrlDecode(Request[\"ids\"]);\r\n");
                batContent.Append("            List<string> idList = ids.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries).ToList();\r\n");

                string template = @"
        private void DeleteData()
        {{
{0}
            {1} dal = new {1}();
            dal.Delete{2}(idList);

            Response.Write(""0"");
        }}
";

                return string.Format(template, batContent.ToString(), PageCache.TableName_DAL, PageCache.TableName);
            }
            else
            {
                return string.Empty;
            }
        }

        private static string CreateDownAndDownAll()
        {
            var down_all_Model = PageCache.GetCmd("导出全部");
            var down_select_Model = PageCache.GetCmd("导出选中");
            if (down_all_Model != null || down_select_Model != null)
            {
                StringBuilder resultContent = new StringBuilder();

                #region 导出全部
                if (down_all_Model != null)
                {
                    StringBuilder searchContent = new StringBuilder();
                    StringBuilder searchStrContent = new StringBuilder();
                    foreach (var item in down_all_Model.AttrList)
                    {
                        string attribute = item.AttrName;
                        string eval_attribute = item.ColName;
                        if (item.DbType.ToLower() == "datetime" ||
                            item.DbType.ToLower() == "date" ||
                            item.DbType.ToLower() == "int" ||
                            item.DbType.ToLower() == "bigint" ||
                            item.DbType.ToLower() == "tinyint")
                        {
                            searchContent.AppendFormat("\t\t\tstring {0}Str = HttpUtility.UrlDecode(Request[\"txtSearch{0}\"]);\r\n", attribute);
                            if (item.DbType.ToLower() == "datetime" || item.DbType.ToLower() == "date")
                            {
                                searchContent.AppendFormat("\t\t\tDateTime {0} = DateTime.MinValue;\r\n", attribute);
                                searchContent.AppendFormat("\t\t\tDateTime.TryParse({0}Str, out {0});\r\n", attribute);
                            }
                            else
                            {
                                searchContent.AppendFormat("\t\t\tint {0} = 0;;\r\n", attribute);
                                searchContent.AppendFormat("\t\t\tint.TryParse({0}Str, out {0});\r\n", attribute);
                            }
                        }
                        else
                        {
                            searchContent.AppendFormat("            string {0} = HttpUtility.UrlDecode(Request[\"txtSearch{0}\"]);\r\n", attribute);
                        }

                        searchStrContent.AppendFormat(" {0},", attribute);
                    }

                    // 移除最后的逗号
                    string searchStrCount = searchStrContent.Length > 0 ? searchStrContent.ToString().Substring(0, searchStrContent.Length - 1) : searchStrContent.ToString();

                    StringBuilder batContent = new StringBuilder();
                    batContent.Append(searchContent.ToString());

                    StringBuilder content = new StringBuilder();
                    content.Append("<table border='1'><thead><tr>");
                    foreach (var item in down_all_Model.AttrList)
                    {
                        content.AppendFormat("<th>{0}</th>", item.TitleName);
                    }

                    content.Append("</tr></thead>");

                    StringBuilder appendFormat = new StringBuilder();
                    foreach (var item in down_all_Model.AttrList)
                    {
                        appendFormat.AppendFormat(@"                content.AppendFormat(""<td>{{0}}</td>"", list[i].{0});{1}", item.AttrName, Environment.NewLine);
                    }

                    string template = @"

        private void DownAll()
        {{
{0}
            {1} dal = new {1}();
            List<{2}> data = dal.GetAll({5});
            string content = CreateTableAll(data);
            Response.Clear(); 
            Response.Buffer = true; 
            Response.Charset = ""UTF-8""; 
            Response.AddHeader(""Content-Disposition"", ""attachment; filename=result.xls""); 
            Response.ContentEncoding = System.Text.Encoding.GetEncoding(""UTF-8""); 
            Response.ContentType = ""application/ms-excel;charset=UTF-8""; 
            Response.Write(content); 
            Response.Flush(); 
            Response.End(); 
        }}

        private string CreateTableAll(List<{2}> list)
        {{
            StringBuilder content = new StringBuilder();
            
            // create columns header
            content.Append(""{3}"");
            for (int i = 0, len = list.Count; i < len; i++)
            {{
                content.Append(""<tr>"");
{4}
                content.Append(""</tr>"");
            }}
            content.Append(""</tbody></table>"");

            return content.ToString();            
        }}";

                    // TODO
                    resultContent.AppendFormat(template, batContent.ToString(), PageCache.TableName_DAL, PageCache.TableName_Model, content.ToString(), appendFormat.ToString(), searchStrCount);
                }
                #endregion

                #region 导出选中
                if (down_select_Model != null)
                {
                    StringBuilder batContent = new StringBuilder();
                    batContent.Append("            string ids = HttpUtility.UrlDecode(Request[\"ids\"]);\r\n");
                    batContent.Append("            List<string> idList = ids.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries).ToList();\r\n");

                    StringBuilder content = new StringBuilder();
                    content.Append("<table border='1'><thead><tr>");
                    foreach (var item in down_select_Model.AttrList)
                    {
                        content.AppendFormat("<th>{0}</th>", item.TitleName);
                    }

                    content.Append("</tr></thead>");

                    StringBuilder appendFormat = new StringBuilder();
                    foreach (var item in down_select_Model.AttrList)
                    {
                        appendFormat.AppendFormat(@"                content.AppendFormat(""<td>{{0}}</td>"", list[i].{0});{1}", item.AttrName, Environment.NewLine);
                    }

                    string template = @"
        private void Down()
        {{
{0}
            {1} dal = new {1}();
            List<{2}> data = dal.GetPartAll(idList);
            string content = CreateTable(data);
            Response.Clear(); 
            Response.Buffer = true; 
            Response.Charset = ""UTF-8""; 
            Response.AddHeader(""Content-Disposition"", ""attachment; filename=result.xls""); 
            Response.ContentEncoding = System.Text.Encoding.GetEncoding(""UTF-8""); 
            Response.ContentType = ""application/ms-excel;charset=UTF-8""; 
            Response.Write(content); 
            Response.Flush(); 
            Response.End(); 
        }}

        private string CreateTable(List<{2}> list)
        {{
            StringBuilder content = new StringBuilder();
            
            // create columns header
            content.Append(""{3}"");
            for (int i = 0, len = list.Count; i < len; i++)
            {{
                content.Append(""<tr>"");
{4}
                content.Append(""</tr>"");
            }}
            content.Append(""</tbody></table>"");

            return content.ToString();            
        }}";

                    // TODO
                    resultContent.AppendFormat(template, batContent.ToString(), PageCache.TableName_DAL, PageCache.TableName_Model, content.ToString(), appendFormat.ToString());
                }
                #endregion

                return resultContent.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        private static string CreateBottom()
        {
            return @"    }
}";
        }
    }
}
