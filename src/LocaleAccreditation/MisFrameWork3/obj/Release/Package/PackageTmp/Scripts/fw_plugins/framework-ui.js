$.reload = function () {
    location.reload();
    return false;
}
$.loading = function (bool, text) {
    var $loadingpage = top.$("#loadingPage");
    var $loadingtext = $loadingpage.find('.loading-content');
    if (bool) {
        $loadingpage.show();
    } else {
        if ($loadingtext.attr('istableloading') == undefined) {
            $loadingpage.hide();
        }
    }
    if (!!text) {
        $loadingtext.html(text);
    } else {
        $loadingtext.html("数据加载中，请稍后…");
    }
    $loadingtext.css("left", (top.$('body').width() - $loadingtext.width()) / 2 - 50);
    $loadingtext.css("top", (top.$('body').height() - $loadingtext.height()) / 2);
}
$.request = function (name) {
    var search = location.search.slice(1);
    var arr = search.split("&");
    for (var i = 0; i < arr.length; i++) {
        var ar = arr[i].split("=");
        if (ar[0] == name) {
            if (unescape(ar[1]) == 'undefined') {
                return "";
            } else {
                return unescape(ar[1]);
            }
        }
    }
    return "";
}

$.currentWindow = function () {
    var iframeId = top.$(".J_iframe:visible").attr("name");
    return top.frames[iframeId];
}
$.browser = function () {
    var userAgent = navigator.userAgent;
    var isOpera = userAgent.indexOf("Opera") > -1;
    if (isOpera) {
        return "Opera"
    };
    if (userAgent.indexOf("Firefox") > -1) {
        return "FF";
    }
    if (userAgent.indexOf("Chrome") > -1) {
        if (window.navigator.webkitPersistentStorage.toString().indexOf('DeprecatedStorageQuota') > -1) {
            return "Chrome";
        } else {
            return "360";
        }
    }
    if (userAgent.indexOf("Safari") > -1) {
        return "Safari";
    }
    if (userAgent.indexOf("compatible") > -1 && userAgent.indexOf("MSIE") > -1 && !isOpera) {
        return "IE";
    };
}
$.download = function (url, data, method) {
    if (url && data) {
        data = typeof data == 'string' ? data : jQuery.param(data);
        var inputs = '';
        $.each(data.split('&'), function () {
            var pair = this.split('=');
            inputs += '<input type="hidden" name="' + pair[0] + '" value="' + pair[1] + '" />';
        });
        $('<form action="' + url + '" method="' + (method || 'post') + '">' + inputs + '</form>').appendTo('body').submit().remove();
    };
};

$.downloadIamge = function (selector, name) {
    // 通过选择器获取img元素
    var img = document.getElementById(selector);
    // 将图片的src属性作为URL地址
    var url = img.src;
    var a = document.createElement('a');
    var event = new MouseEvent('click');

    a.download = name || '图片';
    a.href = url;
    a.dispatchEvent(event);
};

$.print = function (url, data, method) {
    var data2 = data;
    if (url && data) {
        var title = (data.title == null || data.title == undefined) ? '' : data.title;
        data = typeof data == 'string' ? data : jQuery.param(data);
        var inputs = '';
        //$.each(data.split('&'), function () {
        //    var pair = this.split('=');
        //    //inputs += '<input type="hidden" name="' + Search + '" value="' + data2.Search + '" />';
        //});
        if (data2.Search) {
            inputs += '<input type="text" name="Search" value="' + data2.Search + '" />';
        }
        if (data2.date_range_type) {
            inputs += '<input type="text" name="date_range_type" value="' + data2.date_range_type + '" />';
        }
        if (data2.start_date) {
            inputs += '<input type="text" name="start_date" value="' + data2.start_date + '" />';
        }
        if (data2.end_date) {
            inputs += '<input type="text" name="end_date" value="' + data2.end_date + '" />';
        }
        if (data2.cdt_combination) {
            inputs += '<input type="text" name="cdt_combination" value="' + data2.cdt_combination + '" />';
        }
        var html = '<div class="easyui-window"><iframe width="100%" height="100%"  frameborder="0" seamless marginheight="0" style="overflow:hidden;display:block"></iframe></div>';
        var win = window.top.$(html).appendTo(window.top.document.body);
        win.dialog({
            title: "打印预览 " + title,
            fit:true,
            closed: false,
            cache: false,
            striped: true,
            modal: true,
            onClose: function () {
                win.dialog('destroy');//关闭即销毁
            },
            onOpen: function () {
                $iframe = $(this).find('iframe');
                $iframe.contents().find('body').append('<form id="fromdata" action="' + url + '" method="' + (method || 'post') + '">' + inputs + '</form>');
                $iframe.contents().find("#fromdata").submit().remove();
            },
            buttons: [
                {
                    text: '返回',
                    width:100,
                    iconCls: 'icon-undo',
                    handler: function (e) {
                        win.dialog("close");
                    }
                }
            ]
        });
    };
};

$.modalOpen = function (options) {
    var html = "<div class='easyui-window'></div>";
    var win = window.top.$(html).appendTo(window.top.document.body);
    var defaults = {
        id: null,
        title: '系统窗口',
        width: "500px",
        height: "350px",
        url: '',
        callBack: null,
        buttons: [
            {
                id: 'btnForm_OK',
                text: '保存',
                iconCls: 'icon-ok',
                handler: function (e) {
                    options.callBack();
                }
            },
            {
                text: '取消',
                iconCls: 'icon-cancel',
                handler: function (e) {
                    win.dialog("close");
                }
            }
        ]
    };
    var options = $.extend(defaults, options);

    win.dialog({
        id:options.id,
        title: options.title,
        width: options.width,
        height: options.height,
        closed: false,
        cache: false,
        striped: true,
        href: options.url,
        modal: true,
        onClose: function () {
            win.dialog('destroy');//关闭即销毁
        },
        onLoad: function () {
        },
        buttons: options.buttons
    });
}

$.submitForm = function (options) {
    var defaults = {
        url: "",
        param: [],
        loading: "正在提交数据...",
        success: null,
        close: false
    };
    var options = $.extend(defaults, options);
    $.messager.progress({
        msg: options.loading,
        text:''
    });
    window.setTimeout(function () {
        if ($('[name=__RequestVerificationToken]').length > 0) {
            options.param["__RequestVerificationToken"] = $('[name=__RequestVerificationToken]').val();
        }
        $.ajax({
            url: options.url,
            data: options.param,
            type: "post",
            dataType: "json",
            success: function (data) {
                $.messager.progress('close');
                if (data.state) {
                    if (!options.close) {
                        $.messager.alert({
                            icon: 'info',
                            title: '提示',
                            msg: data.message
                        });
                    }
                    else {
                        $.messager.show({
                            msg: data.message,
                            timeout: 3000,
                            showType: 'slide'
                        });
                    }
                    if (options.success != null) {
                        options.success(data);
                    }
                } else {
                    $.messager.alert({
                        icon: 'warning',
                        title: '提示',
                        msg: data.message
                    });
                }
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                $.messager.progress('close');
                $.messager.alert({
                    icon: 'error',
                    title: '提示',
                    msg: errorThrown
                });
            },
            beforeSend: function () {
                
            },
            complete: function () {
                $.messager.progress('close');
            }
        });
    }, 500);
}
$.deleteForm = function (options) {
    var defaults = {
        prompt: "注：您确定要删除该项数据吗？",
        url: "",
        param: [],
        loading: "正在处理数据...",
        width: '400px',
        title: '提示',
        success: null,
        close: true
    };
    var options = $.extend(defaults, options);
    if ($('[name=__RequestVerificationToken]').length > 0) {
        options.param["__RequestVerificationToken"] = $('[name=__RequestVerificationToken]').val();
    }
    $.messager.confirm({
        icon: 'question',
        title: options.title,
        width: options.width,
        msg: options.prompt, 
        fn: function (r) {
            if (r) {
                $.messager.progress({
                    msg: options.loading,
                    text: ''
                });
                window.setTimeout(function () {
                    $.ajax({
                        url: options.url,
                        data: options.param,
                        type: "post",
                        dataType: "json",
                        success: function (data) {
                            $.messager.progress('close');
                            if (data.state) {
                                if (options.success != null) {
                                    options.success(data);
                                }
                            } else {
                                $.messager.alert({
                                    icon: 'warning',
                                    title: '提示',
                                    msg: data.message
                                });
                            }
                        },
                        error: function (XMLHttpRequest, textStatus, errorThrown) {
                            $.messager.progress('close');
                            $.messager.alert({
                                icon: 'error',
                                title: '提示',
                                msg: errorThrown
                            });
                        },
                        beforeSend: function () {

                        },
                        complete: function () {
                            $.messager.progress('close');
                        }
                    });
                }, 500);
            }
        }
    });

}
$.jsonWhere = function (data, action) {
    if (action == null) return;
    var reval = new Array();
    $(data).each(function (i, v) {
        if (action(v)) {
            reval.push(v);
        }
    })
    return reval;
}
$.fn.jqGridRowValue = function () {
    var $grid = $(this);
    var selectedRowIds = $grid.jqGrid("getGridParam", "selarrrow");
    if (selectedRowIds != "") {
        var json = [];
        var len = selectedRowIds.length;
        for (var i = 0; i < len ; i++) {
            var rowData = $grid.jqGrid('getRowData', selectedRowIds[i]);
            json.push(rowData);
        }
        return json;
    } else {
        return $grid.jqGrid('getRowData', $grid.jqGrid('getGridParam', 'selrow'));
    }
}
$.fn.formValid = function () {
    return $(this).valid({
        errorPlacement: function (error, element) {
            element.parents('.formValue').addClass('has-error');
            element.parents('.has-error').find('i.error').remove();
            element.parents('.has-error').append('<i class="form-control-feedback fa fa-exclamation-circle error" data-placement="left" data-toggle="tooltip" title="' + error + '"></i>');
            $("[data-toggle='tooltip']").tooltip();
            if (element.parents('.input-group').hasClass('input-group')) {
                element.parents('.has-error').find('i.error').css('right', '33px')
            }
        },
        success: function (element) {
            element.parents('.has-error').find('i.error').remove();
            element.parent().removeClass('has-error');
        }
    });
}
//序列化 查询条件
$.fn.formSerialize = function () {
    var element = $(this);
    var postdata = {};
    element.find('.easyui-textbox,.easyui-combobox,.easyui-datebox').each(function (r) {
        var $this = $(this);
        var id = $this.attr('textboxname');
        var type = $this.attr('class');
        if (type && type.indexOf("easyui-textbox") > -1)
        {
            postdata[id] = $this.textbox("getValue");
        }
        else if (type && type.indexOf("easyui-combobox") > -1) {
            postdata[id] = $this.combobox("getText");
            if(postdata[id]=='全部')
                postdata[id] = $this.combobox("getValue");
        }
        else if (type && type.indexOf("easyui-datebox") > -1) {
            postdata[id] = $this.datebox("getValue");
        }
        else {
            var value = $this.val() == "" ? "&nbsp;" : $this.val();
            postdata[id] = value;
        }
    });
    if ($('[name=__RequestVerificationToken]').length > 0) {
        postdata["__RequestVerificationToken"] = $('[name=__RequestVerificationToken]').val();
    }
    return postdata;
};

//Combobox和Textbox 联动
$.comboboxSelect = function (id) {
    $('#cb'+id).combobox({
        onSelect: function (rec) {
            $('#txt'+id).textbox('setValue', rec.Code);
        }
    });
    $('#txt'+id).textbox({
        onChange: function (newValue, oldValue) {
            $('#cb'+id).combobox('setValue', newValue);
        }
    }).bind('keydown', function (e) {
        if (e.keyCode == 13) {	// 当按下回车键时接受输入的值。
            $('#cb'+id).combobox('setValue', $(this).val());
        }
    });
}

$.fn.bindSelect = function (options) {
    var defaults = {
        id: "id",
        text: "text",
        search: false,
        url: "",
        param: [],
        change: null
    };
    var options = $.extend(defaults, options);
    var $element = $(this);
    if (options.url != "") {
        $.ajax({
            url: options.url,
            data: options.param,
            dataType: "json",
            async: false,
            success: function (data) {
                $.each(data, function (i) {
                    $element.append($("<option></option>").val(data[i][options.id]).html(data[i][options.text]));
                });
                $element.select2({
                    minimumResultsForSearch: options.search == true ? 0 : -1
                });
                $element.on("change", function (e) {
                    if (options.change != null) {
                        options.change(data[$(this).find("option:selected").index()]);
                    }
                    $("#select2-" + $element.attr('id') + "-container").html($(this).find("option:selected").text().replace(/　　/g, ''));
                });
            }
        });
    } else {
        $element.select2({
            minimumResultsForSearch: -1
        });
    }
}
$.fn.authorizeButton = function () {
    var moduleId = top.$(".NFine_iframe:visible").attr("id").substr(6);
    var dataJson = top.clients.authorizeButton[moduleId];
    var $element = $(this);
    $element.find('a[authorize=yes]').attr('authorize', 'no');
    if (dataJson != undefined) {
        $.each(dataJson, function (i) {
            $element.find("#" + dataJson[i].F_EnCode).attr('authorize', 'yes');
        });
    }
    $element.find("[authorize=no]").parents('li').prev('.split').remove();
    $element.find("[authorize=no]").parents('li').remove();
    $element.find('[authorize=no]').remove();
}
$.fn.dataGrid = function (options) {
    var defaults = {
        datatype: "json",
        autowidth: true,
        rownumbers: true,
        shrinkToFit: false,
        gridview: true
    };
    var options = $.extend(defaults, options);
    var $element = $(this);
    options["onSelectRow"] = function (rowid) {
        var length = $(this).jqGrid("getGridParam", "selrow").length;
        var $operate = $(".operate");
        if (length > 0) {
            $operate.animate({ "left": 0 }, 200);
        } else {
            $operate.animate({ "left": '-100.1%' }, 200);
        }
        $operate.find('.close').click(function () {
            $operate.animate({ "left": '-100.1%' }, 200);
        })
    };
    $element.jqGrid(options);
};

$.AjaxGetData = function (url, datas, successfn) {
    $.ajax({
        type: "get",
        data: datas,
        url: url,
        success: function (data) {//调用接口成功返回方法
            successfn(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {//调用接口失败返回方法
        },
        complete: function (XMLHttpRequest, textStatus) {//调用接口完成后
        }
    });
}
$.AjaxPostData = function (url, datas, successfn) {
    $.ajax({
        type: "post",
        data: datas,
        url: url,
        success: function (data) {//调用接口成功返回方法
            successfn(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {//调用接口失败返回方法
        },
        complete: function (XMLHttpRequest, textStatus) {//调用接口完成后
        }
    });
}