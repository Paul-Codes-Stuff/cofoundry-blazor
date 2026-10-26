// The visual editor's script on the site's pages, a port of the AngularJS admin's ContentPage/GuiController.js.
// It runs alongside the site's own scripts, so it has no dependencies and adds no globals.
//
// It draws the overlays for the page's regions and blocks (from the data-cms-* attributes Cofoundry adds in
// edit mode) and adds the publish buttons to the toolbar. Every admin action is sent to the iframe (the Blazor
// page Components/VisualEditor/VisualEditorFrame.razor) with postMessage, together with the page data, and the
// iframe is shown while its dialog is open. The frame says when it's ready and when its dialog closes.
(function () {
    'use strict';

    var frame, toolbar, blockToolbar, model,
        isFrameReady = false,
        pendingMessages = [];

    function sendToFrame(action, args) {
        var message = { action: action, args: args || {}, context: model };

        frame.style.display = 'block';

        if (isFrameReady) {
            frame.contentWindow.postMessage(message, document.location.origin);
        } else {
            // The frame's circuit may still be connecting.
            pendingMessages.push(message);
        }
    }

    function bindFrame() {
        frame = document.getElementById('cofoundry-src__iFrame');

        window.addEventListener('message', function (e) {
            if (e.origin !== document.location.origin || e.source !== frame.contentWindow || !e.data) return;

            switch (e.data.type) {
                case 'FRAME_READY':
                    isFrameReady = true;
                    pendingMessages.forEach(function (message) {
                        frame.contentWindow.postMessage(message, document.location.origin);
                    });
                    pendingMessages = [];
                    break;
                case 'MODAL_CLOSE':
                    frame.style.display = 'none';
                    break;
            }
        });
    }

    function bindToolbar() {
        var mode = model.visualEditorMode;
        toolbar = document.getElementById('cofoundry-sv');

        if (mode === 'SpecificVersion' && model.canUpdate) {
            addToolbarButton('Copy to<br />draft', 'copyToDraft');
        }

        if ((mode === 'Preview' || mode === 'Edit'
            || (model.publishStatus === 'Unpublished' && model.isLatestPublishedVersion && !model.hasDraftVersion))
            && model.canPublish) {
            addToolbarButton('Publish', 'publish');
        } else if (mode === 'Live' && model.canPublish) {
            addToolbarButton('Unpublish', 'unpublish');
        }
    }

    function addToolbarButton(title, action) {
        var options = toolbar.getElementsByClassName('cofoundry-sv__options')[0],
            button = document.createElement('a'),
            label = document.createElement('span');

        if (!options) return;

        button.className = 'cofoundry-sv__options__button publish popup';
        button.href = '#';
        label.innerHTML = title;
        button.appendChild(label);
        button.addEventListener('click', function (e) {
            e.preventDefault();
            sendToFrame(action);
        });

        options.appendChild(button);
    }

    function bindGui() {
        var addBlockTemplate = document.getElementById('cofoundry-sv__btn-add-block'),
            wrapTemplate = document.getElementById('cofoundry-sv__ui-wrap'),
            wrapContainer = document.body,
            entityType = model.isCustomEntityRoute ? 'custom-entity' : 'page',
            currentElements = [],
            resizeTimer = null,
            scope = { buttons: {} };

        blockToolbar = document.getElementById('cofoundry-sv__block-popover-container');

        // Used by the admin UI styles.
        document.documentElement.className = model.isCustomEntityRoute ? 'cofoundry-editmode__custom-entity' : 'cofoundry-editmode__page';

        if (model.visualEditorMode !== 'Edit' || !blockToolbar) return;

        addUI(document);
        bindBlockButtons();

        window.addEventListener('resize', function () {
            hideBlockToolbar();
            removeUI();
            if (resizeTimer) clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () { addUI(document); }, 500);
        });

        function addUI(rootElement) {
            addForComponent('region', onRegionMouseEnter);
            addForComponent('region-block', onBlockMouseEnter);

            function addForComponent(componentName, onMouseEnter) {
                var selector = 'data-cms-' + entityType + '-' + componentName,
                    elements = rootElement.querySelectorAll('[' + selector + ']'),
                    i;

                for (i = 0; i < elements.length; ++i) {
                    var el = elements[i],
                        data = getElementData(el, componentName),
                        isMultiBlock, wrap, addBlockButton = null;

                    // An empty placeholder block has no name.
                    if (!data.name) continue;

                    isMultiBlock = Boolean((data.isBlock ? el.parentNode : el).getAttribute('data-cms-multi-block'));
                    data.index = i;

                    // An overlay showing the bounds of the editable area.
                    wrap = createWrapElement(data);
                    wrapContainer.appendChild(wrap);

                    // A plus button for adding blocks.
                    if (isMultiBlock || !data.hasContent) {
                        addBlockButton = createAddBlockButton(data);
                        wrapContainer.appendChild(addBlockButton);
                    }

                    el.addEventListener('mouseenter', onMouseEnter);

                    currentElements.push({ el: el, uiElements: [wrap, addBlockButton], onMouseEnter: onMouseEnter });
                }
            }
        }

        function getElementData(el, componentName) {
            var isBlock = componentName === 'region-block',
                regionEl = isBlock ? el.parentNode : el,
                position, data = {};

            if (!el.offsetWidth) {
                el.style.width = '100%';
            }

            position = offset(el);
            data.idName = (isBlock ? 'block' : 'region') + '_ui_wrap';
            data.isBlock = isBlock;
            data.y = position.top;
            data.x = position.left;
            data.width = el.offsetWidth;
            data.height = el.offsetHeight;
            data.hasContent = isBlock ? el.innerHTML.length > 0 : !el.hasAttribute('data-cms-page-region-empty');
            data.name = el.getAttribute(isBlock ? 'data-cms-page-block-title' : 'data-cms-page-region-name');
            data.className = isBlock ? 'cofoundry-sv__ui-wrap--block' : 'cofoundry-sv__ui-wrap--region';

            parseBlockAttributes(el, data);
            parseRegionAttributes(regionEl, data);

            return data;
        }

        function createWrapElement(data) {
            var wrap = wrapTemplate.cloneNode(true);
            wrap.id = data.idName + '_' + data.index;
            wrap.className = data.className;
            wrap.style.top = data.y + 'px';
            wrap.style.left = data.x + 'px';
            wrap.style.width = (data.width - 2) + 'px';
            wrap.style.height = (data.height - 2) + 'px';
            wrap.querySelector('.cofoundry-sv__ui-wrap-name').textContent = data.name;
            return wrap;
        }

        function createAddBlockButton(data) {
            var button = addBlockTemplate.cloneNode(true);
            button.removeAttribute('id');

            if (!data.isBlock && !data.hasContent) {
                button.className += ' cofoundry-sv__btn-add-block--empty';
                button.style.top = data.y + (data.height / 2) + 'px';
            } else {
                button.style.top = (data.isBlock ? (data.y + data.height) : data.y) + 'px';
            }

            button.style.left = data.x + (data.width / 2) + 'px';
            button.style.display = 'block';
            button.title = 'Add content block to ' + data.regionName;
            button.addEventListener('click', function () {
                sendToFrame('addBlock', {
                    insertMode: data.isBlock ? 'AfterItem' : 'First',
                    pageTemplateRegionId: toInt(data.pageTemplateRegionId),
                    permittedBlockTypes: data.permittedBlockTypes,
                    adjacentVersionBlockId: toInt(data.versionBlockId),
                    regionName: data.regionName
                });
            });

            return button;
        }

        function removeUI() {
            currentElements.forEach(function (item) {
                item.el.removeEventListener('mouseenter', item.onMouseEnter);
                item.uiElements.forEach(function (uiElement) {
                    if (uiElement) wrapContainer.removeChild(uiElement);
                });
            });
            currentElements = [];
        }

        function bindBlockButtons() {
            bindButton('cofoundry-sv__btn-block-edit', 'editBlock', function () {
                sendToFrame('editBlock', {
                    versionBlockId: toInt(scope.versionBlockId),
                    pageBlockTypeId: toInt(scope.pageBlockTypeId)
                });
            });
            bindButton('cofoundry-sv__btn-block-moveup', 'moveBlockUp', function () {
                sendToFrame('moveBlock', { versionBlockId: toInt(scope.versionBlockId), isUp: true });
            });
            bindButton('cofoundry-sv__btn-block-movedown', 'moveBlockDown', function () {
                sendToFrame('moveBlock', { versionBlockId: toInt(scope.versionBlockId), isUp: false });
            });
            bindButton('cofoundry-sv__btn-block-addabove', 'addBlockAbove', function () {
                sendAddBlock('BeforeItem');
            });
            bindButton('cofoundry-sv__btn-block-addbelow', 'addBlockBelow', function () {
                sendAddBlock('AfterItem');
            });
            bindButton('cofoundry-sv__btn-block-delete', 'deleteBlock', function () {
                sendToFrame('deleteBlock', { versionBlockId: toInt(scope.versionBlockId) });
            });

            function sendAddBlock(insertMode) {
                sendToFrame('addBlock', {
                    insertMode: insertMode,
                    pageTemplateRegionId: toInt(scope.pageTemplateRegionId),
                    permittedBlockTypes: scope.permittedBlockTypes,
                    adjacentVersionBlockId: toInt(scope.versionBlockId),
                    regionName: scope.regionName
                });
            }
        }

        function bindButton(id, name, handler) {
            var button = document.getElementById(id);
            if (!button) return;
            scope.buttons[name] = button;
            button.addEventListener('click', handler);
        }

        function onRegionMouseEnter(e) {
            parseRegionAttributes(e.target, scope);
        }

        function onBlockMouseEnter(e) {
            var el = e.target,
                position = offset(el);

            parseBlockAttributes(el, scope);
            parseRegionAttributes(el.parentNode, scope);

            showButton('editBlock', scope.versionBlockId);
            showButton('deleteBlock', scope.versionBlockId);
            showButton('moveBlockUp', scope.isMultiBlock);
            showButton('moveBlockDown', scope.isMultiBlock);
            showButton('addBlockAbove', scope.isMultiBlock);
            showButton('addBlockBelow', scope.isMultiBlock);

            blockToolbar.style.display = 'block';
            blockToolbar.style.top = position.top + 'px';
            blockToolbar.style.left = (position.left || 0) + 'px';
        }

        function hideBlockToolbar() {
            blockToolbar.style.display = 'none';
        }

        function showButton(name, condition) {
            var button = scope.buttons[name];
            if (button) button.style.display = condition ? 'block' : 'none';
        }

        function parseRegionAttributes(el, store) {
            store.pageTemplateRegionId = el.getAttribute('data-cms-page-template-region-id');
            store.permittedBlockTypes = parseBlockTypes(el.getAttribute('data-cms-page-region-permitted-block-types'));
            store.regionName = el.getAttribute('data-cms-page-region-name');
            store.isMultiBlock = el.getAttribute('data-cms-multi-block');
        }

        function parseBlockAttributes(el, store) {
            store.versionBlockId = el.getAttribute('data-cms-version-block-id');
            store.pageBlockTypeId = el.getAttribute('data-cms-page-block-type-id');
        }

        function parseBlockTypes(value) {
            return value ? value.split(',') : [];
        }

        function offset(el) {
            var rect = el.getBoundingClientRect();
            return {
                top: rect.top + window.pageYOffset,
                left: rect.left + window.pageXOffset
            };
        }
    }

    function toInt(value) {
        var number = parseInt(value, 10);
        return isNaN(number) ? null : number;
    }

    function init() {
        var dataElement = document.getElementById('cofoundry-sv__data');

        // Static pages get the toolbar only.
        if (!dataElement) return;

        model = JSON.parse(dataElement.textContent);

        bindFrame();
        bindGui();
        bindToolbar();
    }

    // The AngularJS editor waited for 'load' too: the overlays are sized from the laid out page.
    if (document.readyState === 'complete') {
        init();
    } else {
        window.addEventListener('load', init);
    }
})();
