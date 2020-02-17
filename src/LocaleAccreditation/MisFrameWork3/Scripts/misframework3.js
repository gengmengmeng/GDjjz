$.extend($.fn.validatebox.defaults.rules, {
    repasswd: {
        validator: function (value, param) {
            return value == $(param[0]).val();
        },
        message: '两次密码不一致！'
    },
    integer: {
        validator: function (value) {
            return /^[0-9]+$/.test(value);
        },
        message: '请输入整数'
    },
    sfzhm: {
        validator: function (value, param) {
            var arr2 = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2];
            var arr3 = [1, 0, 'X', 9, 8, 7, 6, 5, 4, 3, 2];
            var t = value;
            if (t.length == 18) {
                var arr = t.split('');
                var s;
                var reg = /^\d+$/;
                var pd = 0;
                for (i = 0; i < 17; i++) {
                    if (reg.test(arr[i])) {
                        s = true;
                        pd = arr[i] * arr2[i] + pd;
                    } else {
                        s = false;
                        break;
                    }
                }
                if (s = true) {
                    var r = pd % 11;
                    if (arr[17] == arr3[r]) {
                        return true;
                    } else {
                        return false;
                    }
                }

            } else {
                return false;
            }
        },
        message: '身份证号码格式有误！'
    }
});

ef3 = {
    /** 
    * 以POST表单方式打开新窗口的JQUERY实现 
    */
    openPostWindow:function (url, args, name, windowParam){
        //创建表单对象 
        var _form = $("<form></form>", {
            'id': 'openPostWindowTempForm',
            'method': 'post',
            'action': url,
            'target': name,
            'style': 'display:none'
        }).appendTo($("body"));

        //将隐藏域加入表单 
        for (var i in args) {
            _form.append($("<input>", { 'type': 'hidden', 'name': args[i].id, 'value': args[i].value }));
        }

        //克隆窗口参数对象 
        //默认新窗口配置 
        var windowDefaultConfig = new Object;
        windowDefaultConfig['directories'] = 'no';
        windowDefaultConfig['location'] = 'no';
        windowDefaultConfig['menubar'] = 'no';
        windowDefaultConfig['resizable'] = 'yes';
        windowDefaultConfig['scrollbars'] = 'yes';
        windowDefaultConfig['status'] = 'no';
        windowDefaultConfig['toolbar'] = 'no'; 
        var windowConfig = {};
        $.extend(true, windowConfig, windowDefaultConfig, windowParam);

        //窗口配置字符串 
        var windowConfigStr = "";

        for (var i in windowConfig) {
            windowConfigStr += i + "=" + windowConfig[i] + ",";
        }

        //绑定提交触发事件 
        _form.bind('submit', function () {
            window.open("about:blank", name, windowConfigStr);
        });

        //触发提交事件 
        _form.trigger("submit");
        //表单删除 
        _form.remove();
    },
    datagridToExcel: function (params) {//导出Excel文件
        params.title = "数据列表";
        //getExcelXML有一个JSON对象的配置，配置项看了下只有title配置，为excel文档的标题
        var data = $(cmpDG).datagrid('getExcelXml', params); //获取datagrid数据对应的excel需要的xml格式的内容
        //用ajax发动到动态页动态写入xls文件中
        ef3.openPostWindow("exportExcel", [{ id: 'data', value: data }], "abc", { height: 500, width: 900 });
        return true;
    },
    uploadfile: function (opts) {
        var defaultOpt = {
            uploadJson: "/FWServices/JsonUploadFile",
            fileManagerJson: "/FWServices/JsonUploadFileManager",
            allowFileManager:true,
        }
        var myopts = {};
        $.extend(true, myopts, defaultOpt, opts);
        var editor = KindEditor.editor(myopts);
        editor.loadPlugin('insertfile', function () {
            editor.plugin.fileDialog({
                showRemote: true,
                clickFn: function (url, title) {
                    if (myopts.callback != undefined)
                        myopts.callback(url, title);
                    this.hideDialog();
                }
            });

            if ((opts.file_title != undefined) && (opts.file_title != ""))
                $("#keTitle").val(opts.file_title);
            if ((opts.file_url != undefined) && (opts.file_url != ""))
                $("#keUrl").val(opts.file_url);
        });
    }
}

//控件
$(function ($) {
    //注册上传插件uploadfilebox 支持参数
    $.parser.plugins.push("uploadfilebox");    
    //easyui插件函数
    $.fn.uploadfilebox = function (options, param) {
        if (typeof options == 'string') {
            var method = $.fn.uploadfilebox.methods[options];
            if (method) {
                return method(this, param);
            }
        }
        return this.each(function () {
            var jq = $(this);
            //$.fn.combobox.parseOptions(this)作用是获取页面中的data-options中的配置
            var opts = $.fn.textbox.parseOptions(this);
            $.extend(true, opts, options);
            var myopts = {};
            $.extend(true, myopts, $.fn.uploadfilebox.defaults, opts);
            var btnOption = {
                icons: [
                    {
                        iconCls: 'fa-icon fa fa-file',
                        handler: function (ctl) {
                            var id = ctl.data.target.id;
                            var opt = $("#" + id).uploadfilebox("options");
                            var url = $("#" + id).textbox("getValue");
                            if (url.indexOf("】:") > 1) {
                                title = url.substr(1, url.indexOf("】:") - 1);
                                url = url.substr(url.indexOf("】:") + 2);
                            }
                            if (url != "") {
                                if ((opt.showFile != undefined) && (opt.showFile!=null)) {
                                    opt.showFile(url, title);
                                }
                                else {
                                    window.open(url);
                                }
                            }
                        }
                    },
                    {
                        iconCls: 'fa-icon fa fa-upload',
                        handler: function (ctl) {
                            var id = ctl.data.target.id;
                            var url = $("#" + id).textbox("getValue");
                            var title = "";
                            
                            if (url.indexOf("】:") > 1) {
                                title = url.substr(1, url.indexOf("】:") - 1);
                                url = url.substr(url.indexOf("】:") + 2);
                            }
                            var opt = $("#" + id).uploadfilebox("options");
                            var kindEditorOption = {
                                file_url: url,
                                file_title: title,
                                callback: function (url, title) {
                                    if (opt.afterSelectedFile != undefined) {
                                        opt.afterSelectedFile(url, title, $("#" + id));
                                    }
                                    else {
                                        $.fn.uploadfilebox.defaults.defaultAfterSelectedFile(url, title, $("#" + id));
                                    }
                                }
                            };
                            kindEditorOption.afterUpload = function (url, data, name) {
                                if (opt.autoCloseUploadDialog) {
                                    this.hideDialog();
                                }
                                if (opt.afterUploadSuccess != undefined)
                                    opt.afterUploadSuccess(url, data,$("#" + id));
                            }
                            
                            $.extend(true, kindEditorOption, opt);
                            ef3.uploadfile(kindEditorOption);
                        }
                    }
                ]
            };
            if (myopts.donotshowfile)
                btnOption.icons.splice(0, 1);
                
            $.extend(true, myopts, btnOption);
            $.fn.textbox.call(jq, myopts);
        });
    };
    $.fn.uploadfilebox.methods = {};    
    $.extend(true, $.fn.uploadfilebox.methods, $.fn.textbox.methods, {

    });
    //设置uploadfilebox插件的一些默认值
    $.fn.uploadfilebox.defaults = {
        editable: false,
        donotshowfile: false,
        allowFileManager: true,
        autoCloseUploadDialog: false,
        uploadJson: "/FWServices/JsonUploadFile",
        fileManagerJson: "/FWServices/JsonUploadFileManager",
        defaultAfterSelectedFile: function (url, title, ctl) {
            if (title == url)
                ctl.textbox("setValue", url);
            else
                ctl.textbox("setValue", "【" + title + "】:" + url);
        }
    };




    //注册自定义easyui插件dicshort
    $.parser.plugins.push("dicshort");
    //easyui插件函数
    $.fn.dicshort = function (options, param) {
        //如果options为string，则是方法调用，如$('#divMyPlugin').hello('sayHello');
        if (typeof options == 'string') {
            var method = $.fn.dicshort.methods[options];
            if (method) {
                return method(this, param);
            }
        }
        return this.each(function () {
            var jq = $(this);
            //$.fn.combobox.parseOptions(this)作用是获取页面中的data-options中的配置
            var opts = $.fn.combogrid.parseOptions(this);
            $.extend(true, opts, options);
            var myopts = {};
            $.extend(true, myopts, {}, opts);

            var cmpWidth = myopts.width;
            var labelWidth = myopts.labelWidth;
            var pWidth;
            if (cmpWidth == undefined || labelWidth == undefined || 0 - labelWidth + cmpWidth< 300)
                pWidth = 300;
            else
                pWidth = cmpWidth - labelWidth;
            var dmWidth = 110;
            if (myopts.panelWidth / 4 > dmWidth)
                myopts.panelWidth = myopts.panelWidth / 4;
            var mcWidth = pWidth - 25 - dmWidth;
            $.extend(true, myopts, {
                panelWidth: pWidth,
                panelMaxHeight: 200,
                delay: 800,
                mode: 'remote',
                idField: 'DM',
                textField: 'MC',
                url: 'JsonDicShort?dic=D_MZ',
                multiple: false,
                onChange: function (nv, ov) {
                    console.log("onChange Id="+jq[0].id+"");
                    var clientDic = $("[parentDic='" + jq[0].id + "']");
                    console.log("  >> 子元素个数=" + clientDic.length + "");
                    for (var j = 0; j < clientDic.length; j++) {
                        var jObj = $(clientDic[j]);                        
                        if (jObj.hasClass("easyui-dicshort")) {
                            if (nv=="" || jObj.combogrid("getValue").indexOf(nv)!=0)
                                jObj.combogrid("setValue", "");
                            jObj.combogrid("grid").datagrid("reload");
                        }
                        else if (jObj.hasClass("easyui-diclarge")) {
                            if (nv == "" || jObj.diclarge("getValue").indexOf(nv) != 0)
                                jObj.diclarge("setValue", "");
                        }
                    }
                },
                onBeforeLoad: function (param) {
                    console.log("onBeforeLoad Id=" + jq[0].id);
                    if (jq.attr("parentDic") != undefined) {
                        var pDic = $("#" + jq.attr("parentDic"));
                        var vFilter = "";
                        if (pDic.hasClass("easyui-dicshort")) {
                            vFilter = pDic.combogrid("getValue");
                        }
                        else if (pDic.hasClass("easyui-diclarge")) {
                            //vFilter = pDic.diclarge("getValue");
                            vFilter = pDic.val();
                            console.log("pDic=" + vFilter);
                            console.log(vFilter);
                        }
                        if (vFilter == "")
                            vFilter = "XXXX****XXXX";//过滤所有信息
                        param.filter = vFilter;
                    }
                },
                icons:[{
                    iconCls: 'fa-icon fa fa-remove',
                    handler: function (e) {
                        $("#"+e.data.target.id).dicshort("setValue", "");
                    }
                }],
                columns: [[
                    { field: 'DM', title: '代码', width: dmWidth },
                    { field: 'MC', title: '名称', width: mcWidth }
                ]],
            }, opts);
            if (myopts.parentDic != undefined)
                $("#" + myopts.id).attr("parentDic", myopts.parentDic);
            $.fn.combogrid.call(jq, myopts);
            if (myopts.multiple) {
                var n = this.attributes.textboxname.nodeValue;
                //$("#" + this.id).attr("name", n);
                this.append('<input type="hidden" class="textbox-value" name="'+n+'" value="">');
            }
        });
    };    

    $.fn.dicshort.methods = {};
    $.extend(true, $.fn.dicshort.methods, $.fn.combogrid.methods, {

    });

    //设置dicshort插件的一些默认值
    $.fn.dicshort.defaults = {
        delay: 200,
        mode: 'remote',
        idField: 'DM',
        textField: 'MC',
        url: 'JsonDicShort?dic=D_MZ',
        multiple: false,
        columns: [[
            { field: 'DM', title: '代码', width: 80 },
            { field: 'MC', title: '名称', width: 220 }
        ]],
    };




    //注册自定义diclarge
    $.parser.plugins.push("diclarge");
    //easyui插件函数
    $.fn.diclarge = function (options, param) {
        //如果options为string，则是方法调用，如$('#divMyPlugin').hello('sayHello');
        if (typeof options == 'string') {
            var method = $.fn.diclarge.methods[options];
            if (method) {
                return method(this, param);
            }
        }
        return this.each(function () {
            var jq = $(this);
            //$.fn.combobox.parseOptions(this)作用是获取页面中的data-options中的配置
            var tmpOpt = $.fn.textbox.parseOptions(this);   
            var opts = {};          
            $.extend(true, opts, $.fn.diclarge.defaults, tmpOpt, options);
            var btnClass = 'fa-icon fa fa-search';
            if (opts.dic_multiple_value)
                btnClass = 'fa-icon fa fa-search-plus';
            var cmpId = ("#" + this.id);
            var myopts = {
                editable: true,
                dic_json_url: opts.dic_json_url,
                dic_view_url: opts.dic_view_url,
                onChange: function (nv, ov) {
                    var objId;
                    var objOption;
                    objId = this.id;
                    objOption = $("#" + objId).textbox("options");
                    //同步子字典
                    console.log("onChange LargeDic Id=" + objId + "");
                    var clientDic = $("[parentDic='" + objId + "']");
                    for (var j = 0; j < clientDic.length; j++) {
                        var jObj = $(clientDic[j]);
                        if (jObj.hasClass("easyui-dicshort")) {
                            if (nv == "" || jObj.combogrid("getValue").indexOf(nv) != 0)
                                jObj.combogrid("setValue", "");
                            jObj.combogrid("grid").datagrid("reload");
                        }
                        else if (jObj.hasClass("easyui-diclarge")) {
                            if (nv == "" || jObj.diclarge("getValue").indexOf(nv) != 0)
                                jObj.diclarge("setValue", "");
                        }
                    }
                    if (objOption.load_dm_from_server == undefined || !objOption.load_dm_from_server) {
                        console.log("不从服务端加载代码：diclarge控件【" + objId+"】");
                        return;
                    }
                    if (nv == null || nv == "") {
                        $("#" + objId).textbox("setText", "");
                        return;
                    }
                    if (ov!="" && nv == $("#" + objId).textbox("getText")) {
                        $("#" + objId).textbox("initValue", ov);
                        $("#" + objId).textbox("setText", nv);
                        return;
                    }
                    var v = $("#" + objId).textbox("getValue");
                    if (v == "") {
                        $("#" + objId).textbox("setText", "");
                        return;
                    }
                    //加载数据
                    var postURL = objOption.dic_json_url + "?id=" + v;
                    //console.log("start get data:" + postURL);
                    if (objOption.dic_json_url.indexOf("?")>1)
                        postURL = objOption.dic_json_url + "&id=" + v;
                    //console.log("start get data 改成:" + postURL);
                    $.getJSON(postURL, function (data) {
                        console.log(data);
                        if (data.rows == undefined) {
                            alert("没有查到相应记录");
                            return;
                        }
                        var t = "";
                        for (j = 0; j < data.total; j++) {
                            if (j > 0)
                                t += ",";
                            t += data.rows[j].MC;
                        }
                        $("#" + objId).textbox("setText", t);
                    });
                },
                icons: [
                    {
                        iconCls: 'fa-icon fa fa-remove',
                        handler: function (e) {
                            $(cmpId).textbox("setValue", "");
                        }
                    },
                    {
                        iconCls: btnClass,
                        handler: function (e) {
                            $("#dialogDic").dialog({
                                title: opts.title,
                                width: 680,
                                height: 400,
                                closed: false,
                                cache: false,
                                modal: true,
                                dic_cmp_name: cmpId,
                                dic_multiple_value: opts.dic_multiple_value,
                                dic_json_url: opts.dic_json_url,
                                href: opts.dic_view_url                                
                            });
                        }
                    }                    
                ]
            };
            $.extend(true, myopts, {}, opts);
            $.fn.textbox.call(jq, myopts);   
            if (myopts.parentDic != undefined)
                $("#" + jq[0].id).attr("parentDic", myopts.parentDic);
        });
    };

    $.fn.diclarge.methods = {};
    $.extend(true, $.fn.diclarge.methods, $.fn.textbox.methods, {

    });
    //设置diclarge插件的一些默认值
    $.fn.diclarge.defaults = {
        load_dm_from_server: true,
        title: '字典查询',
        dic_json_url: 'JsonDicLarge?comma_is_or=true&dic=D_MZ',
        dic_view_url: 'ViewDicLargeUI',
        dic_multiple_value: false
    };


    //注册自定义easyui插件uploadpicture
    $.parser.plugins.push("uploadpicture");
    //easyui插件函数
    $.fn.uploadpicture = function (options, param) {
        //如果options为string，则是方法调用，如$('#divMyPlugin').hello('sayHello');
        if (typeof options == 'string') {
            var method = $.fn.uploadpicture.methods[options];
            if (method) {
                return method(this, param);
            }
        }
        return this.each(function () {
            var jq = $(this);
            var opts = $.fn.panel.parseOptions(this);
            $.extend(true, opts, $.fn.uploadpicture.defaults);
            $.extend(true, opts, options);
            
            //构造界面            
            var field_name = opts.field_name;
            var div_footer_name = "upload_picture_" + field_name;
            if ($("#" + div_footer_name).length == 0) {
                var html_footer = '<div id="' + div_footer_name + '"><input type="hidden" id="' + field_name + '" name="' + field_name + '" value="" /><input type="button" id="btnUploadPicture_' + field_name + '" value="选择图片" style="width: 100%;height: 30px;" /></div>';
                $("body").append(html_footer);
            }
            var myopts = {
                tools: [{
                    iconCls: 'icon-clear',
                    handler: function () {
                        $('#' + field_name).val("");
                        $('#' + field_name).change();
                    }
                }
                ],
                onResize: function (w,h) {
                    $('#' + field_name).change();
                },
                footer: '#' + div_footer_name,
                content: '<div id="uploadpicture_imgView_' + field_name + '"></div>',
            };
            $.extend(true, myopts, {}, opts);            
            $.fn.panel.call(jq, myopts);
            //增加事件
            $('#btnUploadPicture_' + field_name).click(function (){
                var editor = KindEditor.editor({
                    uploadJson: myopts.uploadJson,
                    fileManagerJson: myopts.fileManagerJson,
                    allowFileManager: true
                });
                editor.loadPlugin('image', function () {
                    editor.plugin.imageDialog({
                        showRemote: true,
                        imageUrl: $('#' + field_name).val(),
                        clickFn: function (url, title, width, height, border, align) {
                            $("#licensecheck").html("");
                            $('#' + field_name).val(url);
                            $('#' + field_name).change();
                            editor.hideDialog();
                        }
                    });
                });
            });
            //增加输入框的值改变时的事件
            $('#' + field_name).on("change", function () {
                var div = $('#uploadpicture_imgView_' + field_name);
                //获取可用区域的大小
                var w = div.parent().css("width");
                var h = div.parent().css("height");
                w = w.substr(0, w.length - 2);
                h = h.substr(0, h.length - 2);
                w = 0 + w - 6;
                h = 0 + h - 6;
                div.html('');
                var url = $('#' + field_name).val();
                if (url.length>1)
                    div.append('<a href="' + url + '" target="_blank"><img width="' + w + '" height="' + h + '" src="' + url + '"></a>');
            });
        });
    };

    //设置uploadpicture插件的一些默认值
    $.fn.uploadpicture.defaults = {
        uploadJson: "/FWServices/JsonUploadFile",
        fileManagerJson: "/FWServices/JsonUploadFileManager",
    };

    
});