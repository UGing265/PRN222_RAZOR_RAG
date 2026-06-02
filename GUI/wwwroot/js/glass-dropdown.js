function initGlassDropdowns() {
    var selects = document.querySelectorAll('select.form-glass-select, select.form-select');

    selects.forEach(function(select) {
        if (select.dataset.glassDropdownInitialized || select.multiple) return;
        select.dataset.glassDropdownInitialized = 'true';

        select.style.display = 'none';

        var wrapper = document.createElement('div');
        wrapper.className = 'custom-glass-dropdown-wrapper';
        wrapper.style.position = 'relative';
        wrapper.style.width = '100%';

        var button = document.createElement('div');
        button.className = 'custom-glass-dropdown-btn';
        button.style.background = 'rgba(255, 255, 255, 0.65)';
        button.style.backdropFilter = 'blur(8px)';
        button.style.border = '1px solid rgba(82, 39, 27, 0.08)';
        button.style.borderRadius = '14px';
        button.style.padding = '12px 16px';
        button.style.fontSize = '0.92rem';
        button.style.color = 'var(--foreground)';
        button.style.cursor = 'pointer';
        button.style.display = 'flex';
        button.style.justifyContent = 'space-between';
        button.style.alignItems = 'center';
        button.style.transition = 'all 0.25s ease';
        button.style.minHeight = '48px';

        var buttonText = document.createElement('span');
        buttonText.style.flex = '1';
        buttonText.style.whiteSpace = 'nowrap';
        buttonText.style.overflow = 'hidden';
        buttonText.style.textOverflow = 'ellipsis';

        var icon = document.createElement('span');
        icon.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m6 9 6 6 6-6"/></svg>';
        icon.style.transition = 'transform 0.3s ease';

        button.appendChild(buttonText);
        button.appendChild(icon);

        var dropdown = document.createElement('div');
        dropdown.className = 'custom-glass-dropdown-list';
        dropdown.style.position = 'absolute';
        dropdown.style.top = '100%';
        dropdown.style.left = '0';
        dropdown.style.width = '100%';
        dropdown.style.background = 'rgba(255, 255, 255, 0.95)';
        dropdown.style.backdropFilter = 'blur(16px)';
        dropdown.style.border = '1px solid rgba(216, 106, 88, 0.2)';
        dropdown.style.borderRadius = '14px';
        dropdown.style.marginTop = '8px';
        dropdown.style.padding = '8px';
        dropdown.style.boxShadow = '0 12px 30px rgba(74, 31, 22, 0.1)';
        dropdown.style.zIndex = '9999';
        dropdown.style.maxHeight = '250px';
        dropdown.style.overflowY = 'auto';
        dropdown.style.opacity = '0';
        dropdown.style.visibility = 'hidden';
        dropdown.style.transform = 'translateY(-10px)';
        dropdown.style.transition = 'all 0.3s cubic-bezier(0.16, 1, 0.3, 1)';

        var isOpen = false;

        function updateSelection() {
            var selectedOption = select.options[select.selectedIndex];
            buttonText.textContent = selectedOption ? selectedOption.text : 'Select...';
            var children = dropdown.children;
            for (var i = 0; i < children.length; i++) {
                var child = children[i];
                if (child.dataset.value === select.value) {
                    child.style.background = 'var(--coral)';
                    child.style.color = '#fff';
                    child.style.fontWeight = '600';
                } else {
                    child.style.background = 'transparent';
                    child.style.color = 'var(--foreground)';
                    child.style.fontWeight = 'normal';
                }
            }
        }

        function createItem(option) {
            var item = document.createElement('div');
            item.className = 'custom-glass-dropdown-item';
            item.dataset.value = option.value;
            item.textContent = option.text;
            item.style.padding = '10px 14px';
            item.style.borderRadius = '8px';
            item.style.cursor = 'pointer';
            item.style.fontSize = '0.9rem';
            item.style.transition = 'all 0.2s ease';
            item.style.marginBottom = '2px';

            item.addEventListener('mouseenter', function() {
                if (item.dataset.value !== select.value) {
                    item.style.background = 'rgba(216, 106, 88, 0.08)';
                    item.style.color = 'var(--coral)';
                }
            });

            item.addEventListener('mouseleave', function() {
                if (item.dataset.value !== select.value) {
                    item.style.background = 'transparent';
                    item.style.color = 'var(--foreground)';
                }
            });

            item.addEventListener('click', function(e) {
                e.stopPropagation();
                select.value = option.value;
                select.dispatchEvent(new Event('change'));
                updateSelection();
                closeDropdown();
            });

            return item;
        }

        // Populate initial options
        var options = select.options;
        for (var i = 0; i < options.length; i++) {
            dropdown.appendChild(createItem(options[i]));
        }

        // Insert into DOM
        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(select);
        wrapper.appendChild(button);
        wrapper.appendChild(dropdown);

        updateSelection();

        function openDropdown() {
            isOpen = true;
            dropdown.style.opacity = '1';
            dropdown.style.visibility = 'visible';
            dropdown.style.transform = 'translateY(0)';
            button.style.borderColor = 'var(--coral)';
            button.style.boxShadow = '0 6px 18px rgba(216, 106, 88, 0.06)';
            icon.style.transform = 'rotate(180deg)';
        }

        function closeDropdown() {
            isOpen = false;
            dropdown.style.opacity = '0';
            dropdown.style.visibility = 'hidden';
            dropdown.style.transform = 'translateY(-10px)';
            button.style.borderColor = 'rgba(82, 39, 27, 0.08)';
            button.style.boxShadow = 'none';
            icon.style.transform = 'rotate(0deg)';
        }

        button.addEventListener('click', function(e) {
            e.stopPropagation();
            if (isOpen) {
                closeDropdown();
            } else {
                // Close any other open dropdowns
                document.querySelectorAll('.custom-glass-dropdown-list').forEach(function(list) {
                    list.style.opacity = '0';
                    list.style.visibility = 'hidden';
                    list.style.transform = 'translateY(-10px)';
                });
                document.querySelectorAll('.custom-glass-dropdown-btn').forEach(function(btn) {
                    btn.style.borderColor = 'rgba(82, 39, 27, 0.08)';
                    btn.style.boxShadow = 'none';
                    var chevron = btn.querySelector('span:last-child');
                    if (chevron) chevron.style.transform = 'rotate(0deg)';
                });
                openDropdown();
            }
        });

        document.addEventListener('click', function() {
            if (isOpen) closeDropdown();
        });

        select.addEventListener('change', updateSelection);

        // Expose reinit function
        select.reinitGlassDropdown = function() {
            dropdown.innerHTML = '';
            var opts = select.options;
            for (var j = 0; j < opts.length; j++) {
                if (opts[j].style.display === 'none') continue;
                dropdown.appendChild(createItem(opts[j]));
            }
            updateSelection();
        };
    });
}

// Run on DOM ready or immediately if already loaded
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initGlassDropdowns);
} else {
    initGlassDropdowns();
}

window.reinitGlassDropdown = function(selectId) {
    var select = document.getElementById(selectId);
    if (select && select.reinitGlassDropdown) {
        select.reinitGlassDropdown();
    }
};
