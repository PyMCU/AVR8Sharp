project = "Avr8Sharp"
copyright = "2026, Iván Montiel Cardona"
author = "Iván Montiel Cardona"
release = "1.1.0"
version = "1.1"

extensions = [
    "myst_parser",
    "sphinx_copybutton",
    "sphinx_design",
    "sphinx.ext.intersphinx",
]

source_suffix = {
    ".rst": "restructuredtext",
    ".md": "markdown",
}

templates_path = ["_templates"]
exclude_patterns = ["_build", ".venv", "Thumbs.db", ".DS_Store"]

# Served as a sub-path of the shared PyMCU docs site.
html_baseurl = "https://docs.pymcu.org/avr8sharp/"

# ---------------------------------------------------------------------------
# MyST extensions
# ---------------------------------------------------------------------------
myst_enable_extensions = [
    "colon_fence",
    "deflist",
    "tasklist",
    "attrs_inline",
]
myst_heading_anchors = 4

# ---------------------------------------------------------------------------
# HTML / PyData theme (matches the PyMCU docs site)
# ---------------------------------------------------------------------------
html_theme = "pydata_sphinx_theme"
html_static_path = ["_static"]
html_css_files = ["css/custom.css"]
html_title = "Avr8Sharp"

html_theme_options = {
    "navbar_align": "left",
    "navbar_end": ["navbar-icon-links", "theme-switcher"],
    "secondary_sidebar_items": ["page-toc", "edit-this-page"],
    "show_prev_next": True,
    "navigation_with_keys": True,
    "footer_start": ["copyright"],
    "footer_end": ["sphinx-version"],
    "pygments_light_style": "friendly",
    "pygments_dark_style": "monokai",
    "header_links_before_dropdown": 6,
    "navigation_depth": 3,
    "show_nav_level": 1,
    "icon_links": [
        {
            "name": "PyMCU docs",
            "url": "https://docs.pymcu.org",
            "icon": "fa-solid fa-house",
        },
        {
            "name": "GitHub",
            "url": "https://github.com/PyMCU/avr8sharp",
            "icon": "fa-brands fa-github",
        },
        {
            "name": "NuGet",
            "url": "https://www.nuget.org/packages/Avr8Sharp",
            "icon": "fa-solid fa-cube",
        },
    ],
}

html_sidebars = {
    "**": ["sidebar-nav-bs"],
}

html_context = {
    "github_user": "PyMCU",
    "github_repo": "avr8sharp",
    "github_version": "main",
    "doc_path": "docs",
}
