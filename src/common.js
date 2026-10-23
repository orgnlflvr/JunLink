// Jun Link: shared bits for every page (v5.21d). Game-style red loading bars that name each asset, toasts, warning dialogs, the logo.
// Pages use: MDRG.toast(text, 'ok'|'bad'|'info'), await MDRG.ask(text, {ok:'Delete', danger:true}) -> true/false.
(function () {
  if (window.MDRG) return;
  var QUIET = /\/api\/(state|history|diary$|stage\/info|music$|backgrounds|settings$)|\/pet\/|\/api\/pet/;   // background polling: no loading bar for these

  var SKIP = /\/api\/chat|\/pet\/frame|\/api\/pet\/react/;   // long-running (AI replies) or per-frame: never on the loading bar
  var F = 'Montserrat,Segoe UI,system-ui,sans-serif';
  var css = document.createElement('style');
  css.textContent =
    // the game's colours: crimson on deep purple, square blocks with cut corners
    '#mdrg-bar{position:fixed;left:0;top:0;height:3px;width:0;z-index:9999;background:#d3104f;box-shadow:0 0 8px #d3104f;transition:width .25s ease,opacity .3s;opacity:0;pointer-events:none}' +
    '.mdrg-track{position:relative;height:8px;width:90px;background:#150a26;border:1px solid #4a2a7a;overflow:hidden}' +
    '.mdrg-fill{position:absolute;left:0;top:0;bottom:0;width:0;background:repeating-linear-gradient(-45deg,#d3104f 0 8px,#ff2e6a 8px 16px);background-size:22.63px 22.63px;animation:mdrgstripe .6s linear infinite;box-shadow:inset -2px 0 0 #ffc2d3;transition:width .2s ease}' +
    '@keyframes mdrgstripe{to{background-position:22.63px 0}}' +
    '.mdrg-pct{min-width:34px;text-align:right;font-variant-numeric:tabular-nums}' +
    '#mdrg-chip{position:fixed;left:50%;top:10px;z-index:9998;display:flex;align-items:stretch;background:#22103b;color:#fff;font:700 11px/1 ' + F + ';clip-path:polygon(0 0,100% 0,calc(100% - 10px) 100%,0 100%);box-shadow:0 4px 18px #0008;opacity:0;transform:translate(-50%,-8px);transition:.2s;pointer-events:none}' +
    '#mdrg-chip.on{opacity:1;transform:translate(-50%,0)}' +
    '#mdrg-chip .tag{background:#d3104f;padding:8px 10px;letter-spacing:.1em;text-transform:uppercase;font-weight:800}' +
    '#mdrg-chip .mid{display:flex;gap:8px;align-items:center;padding:0 20px 0 10px}' +
    '#mdrg-chip .mdrg-what{color:#b9a3dc;font-weight:600;max-width:220px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}' +
    '#mdrg-cover{position:fixed;inset:0;z-index:10000;background:radial-gradient(ellipse at 50% 30%,#2a0f4a 0%,#0d0517 70%);display:flex;flex-direction:column;align-items:center;justify-content:center;gap:22px;transition:opacity .35s;color:#fff;font-family:' + F + '}' +
    '#mdrg-cover img{max-width:min(280px,70vw);max-height:150px;width:auto;height:auto}' +
    '#mdrg-cover .box{width:min(380px,82vw)}' +
    '#mdrg-cover .hd{display:flex;justify-content:space-between;align-items:baseline;margin-bottom:8px}' +
    '#mdrg-cover .ttl{font-weight:800;font-size:14px;letter-spacing:.16em;text-transform:uppercase;background:#d3104f;padding:4px 14px 4px 10px;clip-path:polygon(0 0,100% 0,calc(100% - 8px) 100%,0 100%)}' +
    '#mdrg-cover .mdrg-pct{font-weight:800;font-size:15px;color:#ff2e6a}' +
    '#mdrg-cover .mdrg-track{width:100%;height:16px;border:2px solid #4a2a7a;clip-path:polygon(8px 0,100% 0,calc(100% - 8px) 100%,0 100%)}' +
    '#mdrg-cover .mdrg-what{margin-top:9px;font-size:12px;font-weight:600;color:#b9a3dc;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}' +
    '#mdrg-cover ul{list-style:none;margin:8px 0 0;padding:0;font-size:11px;line-height:1.6;color:#6f5a92;min-height:70px}' +
    '#mdrg-cover li b{color:#6dc061;font-weight:800;margin-right:6px}#mdrg-cover li.bad b{color:#ff5a7a}' +
    '#mdrg-cover li:first-child{color:#b9a3dc}' +
    '#mdrg-toasts{position:fixed;right:14px;bottom:60px;z-index:10001;display:flex;flex-direction:column;gap:8px;align-items:flex-end;max-width:min(380px,calc(100vw - 28px))}' +
    '.mdrg-toast{padding:10px 14px;border-radius:10px;background:#241733;color:#f1eaff;border:1px solid #3a2a52;border-left:4px solid #a99bc4;font:500 13px/1.4 Montserrat,Segoe UI,system-ui,sans-serif;box-shadow:0 6px 24px #0007;animation:mdrgin .2s ease-out;white-space:pre-wrap}' +
    '.mdrg-toast.ok{border-left-color:#5fd38d}.mdrg-toast.bad{border-left-color:#ff6b8a}.mdrg-toast.info{border-left-color:#ffe23a}' +
    '@keyframes mdrgin{from{opacity:0;transform:translateY(8px)}}' +
    '#mdrg-modal{position:fixed;inset:0;z-index:10002;background:#0009;display:flex;align-items:center;justify-content:center;padding:16px}' +
    '#mdrg-modal .box{background:#241733;color:#f1eaff;border:1px solid #3a2a52;border-radius:14px;max-width:440px;width:100%;padding:18px;font:14px/1.5 Montserrat,Segoe UI,system-ui,sans-serif;box-shadow:0 12px 40px #000a}' +
    '#mdrg-modal .hd{display:flex;gap:10px;align-items:center;font-weight:700;margin-bottom:8px}' +
    '#mdrg-modal .hd img{height:26px;width:auto;max-width:90px}' +
    '#mdrg-modal .bt{display:flex;gap:8px;justify-content:flex-end;margin-top:16px}' +
    '#mdrg-modal button{font:inherit;color:inherit;background:#2a2140;border:1px solid #3a2a52;border-radius:8px;padding:7px 14px;cursor:pointer}' +
    '#mdrg-modal button.go{background:#d6336c;border-color:#d6336c;font-weight:600}' +
    '#mdrg-modal button.go.danger{background:#b3123f;border-color:#ff6b8a}' +
    '.mdrg-logo{height:34px;width:auto;max-width:120px;vertical-align:middle;margin-right:10px;flex:none}';
  document.head.appendChild(css);

  // favicon
  try { var fi = document.createElement('link'); fi.rel = 'icon'; fi.href = '/icon'; document.head.appendChild(fi); } catch (e) { }

  function el(tag, id, html) { var e = document.createElement(tag); if (id) e.id = id; if (html != null) e.innerHTML = html; document.body.appendChild(e); return e; }

  // ---- loading: every asset the page asks the game for is a task; the bar is finished tasks + a slow creep for running ones ----
  var bar, chip, toasts, cover, coverList, pending = 0, chipTimer = null, ticker = null, shown = 0, tasks = [];
  var NAMES = {
    '/api/stories': 'story list', '/api/music': 'music list', '/api/backgrounds': 'background list', '/api/state': 'game state',
    '/api/history': 'chat history', '/api/diary': 'diary', '/api/diary/write': 'writing diary entry', '/api/settings': 'settings',
    '/api/settings/set': 'saving setting', '/api/packs': 'story packs', '/api/packs/make': 'building story pack', '/api/packs/cover': 'pack cover',
    '/api/stage/info': 'stage info', '/api/stage/start': 'opening the stage', '/api/stage/end': 'closing the stage', '/api/stage/cmd': 'stage command', '/api/stage/box': 'text box',
    '/api/story/save': 'saving story', '/api/story/delete': 'deleting story', '/api/story/check': 'checking story', '/api/story/play': 'starting story in the game',
    '/api/music/play': 'music', '/api/pet/info': 'pet', '/logo': 'logo', '/fonts.css': 'fonts'
  };
  function assetName(u) {
    var m = String(u).replace(/^https?:\/\/[^\/]+/, ''), q = '', i = m.indexOf('?');
    if (i >= 0) { q = m.slice(i + 1); m = m.slice(0, i); }
    if (m === '/api/story') { var id = null; try { id = new URLSearchParams(q).get('id'); } catch (e) { } return id ? 'story "' + id + '"' : 'story'; }
    return NAMES[m] || m.replace(/^\/(api\/)?/, '').replace(/[\/_-]+/g, ' ') || 'page';
  }
  function progress() {
    if (!tasks.length) return 100;
    var d = 0; for (var i = 0; i < tasks.length; i++) d += tasks[i].done ? 1 : tasks[i].v;
    return Math.min(pending ? 99 : 100, Math.round(d / tasks.length * 100));
  }
  function label() {
    var done = 0, cur = null;
    for (var i = 0; i < tasks.length; i++) { if (tasks[i].done) done++; else cur = tasks[i]; }
    return (cur ? cur.name : 'done') + '  \u00b7  ' + done + '/' + tasks.length;
  }
  function paint() {
    shown = Math.max(shown, progress());
    if (bar) bar.style.width = shown + '%';
    var q = function (sel, f) { var l = document.querySelectorAll(sel); for (var i = 0; i < l.length; i++) f(l[i]); };
    q('.mdrg-fill', function (e) { e.style.width = shown + '%'; });
    q('.mdrg-pct', function (e) { e.textContent = shown + '%'; });
    var t = label(); q('.mdrg-what', function (e) { e.textContent = t; });
  }
  function tick() { for (var i = 0; i < tasks.length; i++) if (!tasks[i].done) tasks[i].v += (0.9 - tasks[i].v) * 0.07; paint(); }
  function begin(name) {
    if (pending === 0) { tasks = []; shown = 0; }
    var t = { name: name, v: 0, done: false };
    tasks.push(t); pending++;
    if (!ticker) ticker = setInterval(tick, 100);
    update(); return t;
  }
  function end(t, ok) {
    if (!t || t.done) return;
    t.done = true; pending = Math.max(0, pending - 1);
    if (coverList) {
      var li = document.createElement('li'); if (!ok) li.className = 'bad';
      li.innerHTML = '<b>' + (ok ? '&#10003;' : '&#10007;') + '</b>'; li.appendChild(document.createTextNode(t.name));
      coverList.insertBefore(li, coverList.firstChild);
      while (coverList.children.length > 4) coverList.removeChild(coverList.lastChild);
    }
    update();
  }
  function update() {
    if (!bar) return;
    if (pending > 0) {
      bar.style.opacity = '1'; paint();
      if (!chipTimer) chipTimer = setTimeout(function () { if (pending > 0 && !cover) chip.classList.add('on'); }, 350);
    } else {
      clearInterval(ticker); ticker = null; paint();
      setTimeout(function () { if (pending === 0) { bar.style.opacity = '0'; setTimeout(function () { if (pending === 0) bar.style.width = '0'; }, 300); } }, 250);
      clearTimeout(chipTimer); chipTimer = null; setTimeout(function () { if (pending === 0) chip.classList.remove('on'); }, 250);
    }
  }

  // every fetch drives the loading bar, except background polling once the page is up (and AI replies / pet frames, ever)
  var realFetch = window.fetch.bind(window);
  window.fetch = function (url, opts) {
    var u = typeof url === 'string' ? url : (url && url.url) || '';
    var post = !!(opts && opts.method === 'POST');
    var counted = !SKIP.test(u) && (post || !QUIET.test(u) || !!cover || !bar);
    var t = counted ? begin(assetName(u)) : null;
    return realFetch(url, opts).then(function (r) { end(t, r.ok); return r; },
      function (e) { if (t) { end(t, false); toast('The game is not answering. Is it running with the mod?', 'bad'); } throw e; });
  };

  // fonts count as an asset too
  try { if (document.fonts && document.fonts.ready) { var ft = begin('fonts'); document.fonts.ready.then(function () { end(ft, true); }, function () { end(ft, false); }); } } catch (e) { }

  function toast(text, kind, ms) {
    if (!toasts) return;
    var t = document.createElement('div'); t.className = 'mdrg-toast ' + (kind || 'info'); t.textContent = text;
    toasts.appendChild(t);
    setTimeout(function () { t.style.transition = '.3s'; t.style.opacity = '0'; setTimeout(function () { t.remove(); }, 300); }, ms || (kind === 'bad' ? 6000 : 3000));
  }

  function ask(text, o) {
    o = o || {};
    return new Promise(function (done) {
      var m = el('div', 'mdrg-modal');
      m.innerHTML = '<div class="box" role="dialog" aria-modal="true"><div class="hd"><img src="/logo" alt=""><span></span></div><div class="tx"></div>' +
        '<div class="bt"><button class="no"></button><button class="go' + (o.danger ? ' danger' : '') + '"></button></div></div>';
      m.querySelector('.hd span').textContent = o.title || (o.danger ? 'Are you sure?' : 'Heads up');
      m.querySelector('.tx').textContent = text;
      m.querySelector('.no').textContent = o.cancel || 'Cancel';
      var go = m.querySelector('.go'); go.textContent = o.ok || 'OK';
      function close(v) { document.removeEventListener('keydown', key); m.remove(); done(v); }
      function key(e) { if (e.key === 'Escape') close(false); if (e.key === 'Enter') close(true); }
      m.querySelector('.no').onclick = function () { close(false); };
      go.onclick = function () { close(true); };
      m.onclick = function (e) { if (e.target === m) close(false); };
      document.addEventListener('keydown', key);
      setTimeout(function () { (o.danger ? m.querySelector('.no') : go).focus(); }, 0);
    });
  }

  // small pixel-art logos: scale up sharp, and show bigger on the loading screen
  document.addEventListener('load', function (e) {
    var i = e.target;
    if (!i || i.tagName !== 'IMG' || !/\/logo$/.test(i.src)) return;
    if (i.naturalWidth <= 160 && i.naturalHeight <= 160) {
      i.style.imageRendering = 'pixelated';
      if (i.closest('#mdrg-cover')) { i.style.height = '128px'; i.style.width = 'auto'; }
    }
  }, true);

  function logo() {
    if (document.querySelector('.mdrg-logo') || document.body.dataset.nologo != null) return;
    var h = document.querySelector('h1') || document.querySelector('h2');
    if (!h) return;
    var i = document.createElement('img'); i.className = 'mdrg-logo'; i.src = '/logo'; i.alt = 'Jun Link';
    if (getComputedStyle(h).display === 'block') { h.style.display = 'flex'; h.style.alignItems = 'center'; }
    h.insertBefore(i, h.firstChild);
  }

  function start() {
    bar = el('div', 'mdrg-bar');
    chip = el('div', 'mdrg-chip', '<span class="tag">Loading</span><span class="mid"><span class="mdrg-track"><span class="mdrg-fill"></span></span><span class="mdrg-what"></span><span class="mdrg-pct">0%</span></span>');
    toasts = el('div', 'mdrg-toasts');
    if (document.body.dataset.nocover == null) {
      cover = el('div', 'mdrg-cover', '<img alt="Jun Link"><div class="box"><div class="hd"><span class="ttl">Loading assets</span><span class="mdrg-pct">0%</span></div>' +
        '<div class="mdrg-track"><span class="mdrg-fill"></span></div><div class="mdrg-what"></div><ul></ul></div>');
      coverList = cover.querySelector('ul');
      var img = cover.querySelector('img'), lt = begin('logo');
      img.onload = function () { end(lt, true); }; img.onerror = function () { end(lt, false); }; img.src = '/logo';
      var t0 = Date.now();
      var hide = function () {
        if (!cover) return;
        if ((pending > 0 && Date.now() - t0 < 6000) || Date.now() - t0 < 450) { setTimeout(hide, 120); return; }
        paint(); var c = cover; cover = null; coverList = null;
        setTimeout(function () { c.style.opacity = '0'; setTimeout(function () { c.remove(); }, 400); }, 250);
      };
      setTimeout(hide, 250);
    }
    update();
    logo();
  }
  if (document.body) start(); else document.addEventListener('DOMContentLoaded', start);

  // update notice: the mod asks GitHub once when the game starts; if a newer release is out, say so once per browser session
  function updateNotice() {
    realFetch('/api/state').then(function (r) { return r.json(); }).then(function (s) {
      if (!s || !s.update || !toasts) return;
      var key = 'mdrg-update-' + s.mod + '-' + s.update;
      try { if (sessionStorage.getItem(key)) return; sessionStorage.setItem(key, '1'); } catch (e) { }
      var t = document.createElement('div'); t.className = 'mdrg-toast info';
      t.appendChild(document.createTextNode(s.mod + ' ' + s.update + ' is out (you have ' + s.version + '). '));
      var a = document.createElement('a'); a.href = s.updateUrl; a.target = '_blank'; a.rel = 'noopener'; a.textContent = s.updatePrivate ? 'See what changed' : 'Download';
      a.style.color = 'inherit'; a.style.fontWeight = '700'; t.appendChild(a);
      toasts.appendChild(t);
      setTimeout(function () { t.style.transition = '.3s'; t.style.opacity = '0'; setTimeout(function () { t.remove(); }, 300); }, 15000);
    }).catch(function () { });
  }
  setTimeout(updateNotice, 1500);

  window.MDRG = { toast: toast, ask: ask, begin: begin, end: end };   // begin(name) -> task, end(task, ok): show your own work on the bar
})();
