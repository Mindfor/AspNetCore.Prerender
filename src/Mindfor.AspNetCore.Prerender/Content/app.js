var system = require("system");
var webserver = require("webserver");
var webpage = require("webpage");
var defaultTimeout = 2000;

// get port
var args = system.args;
if (args.length == 1) {
	throw new Error("Server port is not specified");
	phantom.exit();
}

var port = parseInt(args[1]);
if (isNaN(port)) {
	console.error("Invalid port: " + args[1]);
	phantom.exit(1)
}

// start server
var server = webserver.create();
server.listen(port, serverHandler);
console.log("[Port:" + server.port + "]");
console.log("[Initialized]");

// global methods
function serverHandler(request, response) {
	var url = request.url.substr(1);
	console.log("Prerendering: " + url);

	prerender(url, null, function (result) {
		if (result.isSuccess) {
			console.log("Success: " + url + ", " + result.time + "ms");
			response.statusCode = 200;
		}
		else {
			console.log("Fail: " + url + ", " + error);
			response.statusCode = 500;
		}

		response.setHeader("Content-Type", "text/json; charset=UTF-8")
		response.write(JSON.stringify(result));
		response.close();
	});
}

function prerender(url, timeout, callback) {
	timeout = timeout || defaultTimeout;
	var start = new Date();
	var page = createPage();
	var timeoutObject = null;

	page.onCallback = function (data) {
		if (data == "completed")
			success(true);
	};
	
	page.open(url, function (status) {
		if (status == "success")
			timeoutObject = setTimeout(success, timeout);
		else
			fail();
	});

	function success(isCallback) {
		var time = Date.now() - start;
		if (timeoutObject) {
			clearTimeout(timeoutObject);
			timeoutObject = null;
		}

		page.evaluate(removeScriptTags);
		var result = {
			isSuccess: true,
			time: time,
			isCallback: isCallback,
			statusCode: page.status,
			headers: page.headers,
			content: page.content
		};

		page.close();
		callback(result);
	}

	function fail() {
		var time = Date.now() - start;
		var result = {
			isSuccess: false,
			time: time,
			error: page.reason
		};

		page.close();
		callback(result);
	}
}

function createPage() {
	var page = webpage.create();
	page.settings.userAgent = "Mozilla/5.0 Chrome/10.0.613.0 Safari/534.15 PrerenderBot";
	page.settings.loadImages = false;
	page.settings.resourceTimeout = 30000;
	page.viewportSize = { width: 1024, height: 768 };

	page.onResourceReceived = function (response) {
		if (response.stage !== "end")
			return;
		if (response.url == page.url) {
			page.status = response.status;
			page.headers = response.headers;
		}
	};

	page.onResourceError = function (resourceError) {
		page.reason = resourceError.errorString;
		page.reasonUrl = resourceError.url;
	};
	return page;
}

function removeScriptTags() {
	Array.prototype.slice.call(document.getElementsByTagName("script"))
		.filter(function (script) {
			return script.type != "application/ld+json";
		})
		.forEach(function (script) {
			script.parentNode.removeChild(script);
		});
}