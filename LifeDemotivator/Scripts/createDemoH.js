var imageLoader = document.getElementById('imageLoader');
imageLoader.addEventListener('change', handleImage, false);
var canvas = document.getElementById('imageCanvas');
var ctx = canvas.getContext('2d');

ctx.fillRect(0, 0, 700, 500);


function handleImage(e) {
    var reader = new FileReader();
    reader.onload = function (event) {
        var img = new Image();
        img.onload = function () {
            ctx.drawImage(img, 55, 50, 598, 236);
        }
        img.src = event.target.result;
    }

    reader.readAsDataURL(e.target.files[0]);
}
function headerText() {
    var hstr = document.getElementById("headerStr").value;
    ctx.fillStyle = "white";
    ctx.font = "60px Arial white";
    ctx.textAlign = "center";
    ctx.fillText(hstr, 350, 380);
    var str = document.getElementById("Str").value;
    ctx.fillStyle = "white";
    ctx.font = "30px Arial white";
    ctx.textAlign = "center";
    ctx.fillText(str, 350, 450);
}

function demoToServer() {
    var image = canvas.toDataURL("image/png");
    image = image.replace('data:image/png;base64,', '');
    $.ajax({
        async: "false",
        type: 'POST',
        url: 'SaveDemotivator',
        data: '{ "imageData" : "' + image + '" }',

        contentType: 'application/json; charset=utf-8',

        dataType: 'json',
        success: function (msg) {

            alert('Image saved successfully !');

        }
    });


};