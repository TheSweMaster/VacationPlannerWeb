//Logic for Date picker on client side

function FromDateValidation() {
    var fromdate = $("#FromDate").val();
    var todate = $("#ToDate").val();
    var d = new Date();
    var compDate = new Date();
    compDate.setDate(d.getDate() - 1);

    if (Date.parse(todate) < Date.parse(fromdate)) {
        $("#ToDate").val(fromdate);
    }
    if (Date.parse(fromdate) <= compDate) {
        $("span[data-valmsg-for='FromDate']").html("Are you sure you want to book a vacation in the past?");
    }
}

function ToDateValidation() {
    var fromdate = $("#FromDate").val();
    var todate = $("#ToDate").val();
    if (Date.parse(todate) < Date.parse(fromdate)) {
        $("#FromDate").val(todate);
    }
}
