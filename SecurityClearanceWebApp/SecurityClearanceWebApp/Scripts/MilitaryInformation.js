$(".service-number-input").bind("change", function (evennt) {

    

    //check error
    if (checkErrors($(this)) == "error") {
        return;
    }

    // show progress
    showProgressBar($(this));


    showServiceInfo($(this));

});



$(".service-number-input").bind("keyup", function (e) {
    var serviceNumber = $(this).val().trim();
    serviceNumber = serviceNumber.toUpperCase();
    $(this).val(serviceNumber);
    
});

  

var request_url = "";

function setRequestUrl(url) {
    request_url = url;
}

function checkErrors(inputContact) {
    //remove old html
    $(inputContact).next(".template-root").html("");

    //get the value to check it
    var serviceNumber = $(inputContact).val().trim();

    if (serviceNumber == '' || serviceNumber.length <= 0) {
        showErrorMessage($(inputContact), "insert service number")
        return "error";
    }

    //start with D
    //if (!serviceNumber.match("^D")) {
    //    showErrorMessage($(inputContact), "must be start with capital D")
    //    return "error";
    //}
    //character after D is number
    //if (!$.isNumeric(serviceNumber.charAt(1))) {
    //    showErrorMessage($(inputContact), "the character after D should be number")
    //    return "error";
    //}
    //serviceNumbering has -
    if (serviceNumber.indexOf('-') == -1) {
        showErrorMessage($(inputContact), "the service number must contain - ")
        return "error";
    }

    //all c after - is number
    var numberPart = serviceNumber.substr(serviceNumber.indexOf("-") + 1);
    if (!$.isNumeric(numberPart)) {
        showErrorMessage($(inputContact), "all  characters after '-' must be number ")
        return "error";
    }

    //if (serviceNumber.length < 5) {
    //    showErrorMessage($(inputContact), "the service number must be more than 5 characters")
    //    return "error";
    //}

    return "success";
}

function showErrorMessage(inputContact, message) {
    //remove old html
    $(inputContact).next(".template-root").remove();
    var template = $($("#hidden-template-not-found").html());
    $(".alert-message", template).text(message);
    var templateHtml = template.html();
    $(templateHtml).insertAfter(inputContact);
}

function showProgressBar(inputContact) {
    //remove old html
    $(inputContact).next(".template-root").remove();
    var template = $($("#hidden-template-progress").html());
    var templateHtml = template.html();
    $(templateHtml).insertAfter(inputContact);
}


function showServiceInfo(inputContact) {
    $.ajax({
        url: request_url,
        dataType: 'json',
        type: 'post',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify({ "sNo": inputContact.val() }),
        success: function (data) {
            if (lang == 'ar') {
                $("#user-details-lisst").css("text-align", "right");
            } else {
                $("#user-details-lisst").css("text-align", "left");
            }
            //console.log(data);
            if (data.status === "1") {
                //remove old html
                $(inputContact).next(".template-root").remove();
                var template = $($("#hidden-template").html());
                if (data.info.NAME_EMP_A) {
                    if (lang == 'ar') {
                        $(".name", template).text("الإسم : " + data.info.NAME_EMP_A);
                    } else {
                        $(".name", template).text("Name : " + data.info.NAME_EMP_E);
                    }
                   
                }
                if (data.info.EMP_SERVICE_NO) {
                    if (lang == 'ar') {
                        $(".s_no", template).text("الرقم العسكري : " + data.info.EMP_SERVICE_NO);
                    } else {
                        $(".s_no", template).text("Service Number : " + data.info.EMP_SERVICE_NO);
                    }
                }
                if (data.info.NAME_TRADE_A) {
                    if (lang == 'ar') {
                        $(".trade", template).text("المنصب : " + data.info.NAME_TRADE_A);
                    } else {
                        $(".trade", template).text("Position : " + data.info.NAME_TRADE_E);
                    }
                }
                if (data.info.NAME_RANK_A) {
                    if (lang == 'ar') {
                        $(".rank", template).text("الرتبة : " + data.info.NAME_RANK_A);
                    } else {
                        $(".rank", template).text("Rank : " + data.info.NAME_RANK_E);
                    }
                }
                if (data.info.NAME_UNIT_A) {
                    if (lang == 'ar') {
                        $(".unit", template).text("القسم : " + data.info.NAME_UNIT_A);
                    } else {
                        $(".unit", template).text("Unit : " + data.info.NAME_UNIT_E);
                    }
                }
                if (data.info.NAME_POST_A) {
                    if (lang == 'ar') {
                        $(".position", template).text("الوحدة : " + data.info.NAME_POST_A);
                    } else {
                        $(".position", template).text("Unit : " + data.info.NAME_POST_E);
                    }
                }


                $(".user_image", template).prop("src", "http://mamrafowebgov01/images/" + data.info.EMP_SERVICE_NO + ".gif");
                var templateHtml = template.html();
                $(templateHtml).insertAfter(inputContact);
            } else {
                showErrorMessage(inputContact, "we didn't found this user in our records");
            }

        },
        error: function (jqXhr, textStatus, errorThrown) {
            //console.log(errorThrown);
            showErrorMessage(inputContact, "we didn't found this user in our records");
        }
    });
}