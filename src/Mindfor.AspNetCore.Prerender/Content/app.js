var system = require("system");
var webserver = require("webserver");
var webpage = require("webpage");
var defaultTimeout = 2000;

// get arguments
var portStr;
if (system.args.length > 1)
	portStr = system.args[1];
else if (system.env.PORT)
	portStr = system.env.PORT;
else {
	console.error("Server port is not specified");
	phantom.exit(1);
}

var port = parseInt(portStr);
if (isNaN(port)) {
	console.error("Invalid port: " + portStr);
	phantom.exit(1)
}

var securityKey = system.env.PRERENDER_KEY;
if (securityKey)
	securityKey = securityKey.toLowerCase();

// start server
var server = webserver.create();
server.listen(port, serverHandler);
if (!server.port) {
	console.error("Unable to start server at port: " + port);
	phantom.exit(1);
}

console.log("Port: " + server.port);
console.log("Initialized");

// server method
function serverHandler(request, response) {
	// get params from GET or POST requst
	var params;
	if (request.method == "GET") {
		if (request.url === "/") {
			writeResponse(response, 200, "PhantomJS prerender");
			return;
		}

		params = parseQueryString(request.url);
	}
	else if (request.method == "POST") {
		if (!request.post) {
			writeResponse(response, 400, "Body is not specified");
			return;
		}

		try {
			params = JSON.parse(request.post);
		}
		catch (ex) {
			writeResponse(response, 400, ex.message);
			return;
		}
	}
	else {
		writeResponse(response, 400, "Method " + request.method + " is not supported");
		return;
	}

	// validate parameters
	var prerenderUrl = params.url;
	if (prerenderUrl == null) {
		writeResponse(response, 400, "Prerender URL is not specified");
		return;
	}
	else if (!isAbsoluteUrl(prerenderUrl)) {
		writeResponse(response, 400, "Prerender URL must be absolute with \"http\" or \"https\" scheme");
		return;
	}

	var timeout = defaultTimeout;
	if ("timeout" in params) {
		timeout = parseInt(params.timeout);
		if (isNaN(timeout) || timeout <= 0) {
			writeResponse(response, 400, "Timeout must be positive integer");
			return;
		}
	}

	if (securityKey) {
		var key;
		if (params.key)
			key = params.key.toLowerCase();

		if (key !== securityKey) {
			writeResponse(response, 400, "Security key does not match");
			return;
		}
	}

	// prerender
	console.log("Prerendering: " + prerenderUrl);
	prerender(prerenderUrl, timeout, function (result) {
		if (result.isSuccess) {
			console.log("Success: " + prerenderUrl + ", " + result.time + "ms");
			response.statusCode = 200;
		}
		else {
			console.log("Fail: " + prerenderUrl + ", " + error);
			response.statusCode = 500;
		}

		response.setHeader("Content-Type", "text/json; charset=UTF-8")
		response.write(JSON.stringify(result));
		response.close();
	});
}

function writeResponse(response, statusCode, text) {
	response.statusCode = statusCode;
	response.setHeader("Content-Type", "text/plain; charset=UTF-8")
	response.write(text);
	response.close();
}

// PhantomJS prerender method
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

// helper methods
function isAbsoluteUrl(url) {
	return /^https?:\/\//i.test(url);
}

function parseQueryString(url) {
	var index = url.indexOf("?");
	if (index == -1)
		return {};

	var qs = url.substr(index + 1);
	var obj = {};
	qs.split("&").forEach(function (str) {
		index = str.indexOf("=");
		if (index == -1)
			obj[str] = null;
		else {
			var key = str.substr(0, index);
			var value = str.substr(index + 1);
			obj[key] = value;
		}
	});
	return obj;
}