﻿// Write your Javascript code.

function LocalBackup() {
    $("#local-backup .loader").show();
    $.ajax({
        url: 'Backup/LocalBackup',
        success: function (data) {
            $("#local-backup .loader").hide();
            $("#local-backup .glyphicon-ok").show();
            ClouldBackup(data);
        },
        error: function (data) {
            $("#local-backup .loader").hide();
            $("#local-backup .glyphicon-remove").show();
        }
    });
}

function ClouldBackup(path) {
    $("#cloud-uploader .loader").show();
    $.ajax({
        url: 'Backup/CloudBackup?path=' + path,
        success: function (data) {
            $("#cloud-uploader .loader").hide();
            $("#cloud-uploader .glyphicon-ok").show();
        },
        error: function (data) {
            $("#cloud-uploader .loader").hide();
            $("#cloud-uploader .glyphicon-remove").show();
        }
    });
}

function GetLaizyTags(value) {
    var separator = ",";
    var lastComma = value.lastIndexOf(separator);
    var searchTerm = (lastComma > -1) ? value.substring(lastComma + separator.length) : value;
    $.ajax({
        type: "GET",
        data: searchTerm === "" ? null : "startWith=" + searchTerm,
        url: 'Helper/LaizyTags',
        success: function (result) {
            PopulateLaizyTags(result);
        },
        error:function(data) {
            
        }
    });
}

function AddToLine(str) {
    var el = $(".Tags");
    var value = el[0].value;
    var separator = ",";
    var lastComma = value.lastIndexOf(separator);
    var text = (lastComma > -1) ? value.substring(0,lastComma+1) : "";

    el[0].value = text + str;
    HideDropdown();
    el.focus();
}

function PopulateLaizyTags(result) {
    
    $(".laizyTags").empty();

    result.forEach(function (v) {
        var li = document.createElement('li');
        var a = document.createElement('a');
        a.href = "#";
        a.text = v;
        a.onclick = function () {
            AddToLine(v);
        }
        li.append(a);
        $(".laizyTags").append(li);
        //dropdown += '<li onclick="AddToLine(\'' + v +'\')"><a href="#">' + v + '</a></li>';
    });

    $(".laizyTags").show();
}

function HideDropdown() {
    $(".laizyTags").hide();
}
