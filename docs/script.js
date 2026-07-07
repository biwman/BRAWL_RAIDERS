document.documentElement.classList.add("js-enabled");

const pageMeta = {
  en: {
    title: "STARJACKERS | Space raid for loot and survival",
    description: "STARJACKERS is a space action game about raiding sectors, hauling cargo, crafting gear, upgrading ships, and escaping before the loot is lost."
  },
  pl: {
    title: "STARJACKERS | Kosmiczny raid o łup i przetrwanie",
    description: "STARJACKERS to kosmiczna gra akcji o sektorach, cargo, craftingu, statkach, gadżetach i ucieczce przed utratą łupu."
  }
};

const header = document.querySelector("[data-header]");
const menuToggle = document.querySelector("[data-menu-toggle]");
const nav = document.querySelector("[data-nav]");
const dialog = document.querySelector("[data-dialog]");
const dialogImage = document.querySelector("[data-dialog-image]");
const dialogTitle = document.querySelector("[data-dialog-title]");
const dialogClose = document.querySelector("[data-dialog-close]");
const languageButtons = document.querySelectorAll("[data-lang-option]");

let currentLanguage = localStorage.getItem("starjackers-language") || "en";
if (!pageMeta[currentLanguage]) {
  currentLanguage = "en";
}

function syncHeader() {
  if (!header) {
    return;
  }

  header.classList.toggle("is-scrolled", window.scrollY > 12);
}

function setLanguage(language) {
  currentLanguage = pageMeta[language] ? language : "en";
  localStorage.setItem("starjackers-language", currentLanguage);
  document.documentElement.lang = currentLanguage;
  document.title = pageMeta[currentLanguage].title;

  const description = document.querySelector("meta[name='description']");
  const ogDescription = document.querySelector("meta[property='og:description']");
  if (description) {
    description.setAttribute("content", pageMeta[currentLanguage].description);
  }
  if (ogDescription) {
    ogDescription.setAttribute("content", pageMeta[currentLanguage].description);
  }

  document.querySelectorAll("[data-en][data-pl]").forEach((node) => {
    const value = node.getAttribute(`data-${currentLanguage}`);
    if (value !== null) {
      node.textContent = value;
    }
  });

  languageButtons.forEach((button) => {
    const isActive = button.getAttribute("data-lang-option") === currentLanguage;
    button.classList.toggle("is-active", isActive);
    button.setAttribute("aria-pressed", String(isActive));
  });
}

syncHeader();
setLanguage(currentLanguage);

window.addEventListener("scroll", syncHeader, { passive: true });

if (menuToggle && nav) {
  menuToggle.addEventListener("click", () => {
    const isOpen = nav.classList.toggle("is-open");
    menuToggle.setAttribute("aria-expanded", String(isOpen));
  });

  nav.addEventListener("click", (event) => {
    if (event.target instanceof HTMLAnchorElement) {
      nav.classList.remove("is-open");
      menuToggle.setAttribute("aria-expanded", "false");
    }
  });
}

languageButtons.forEach((button) => {
  button.addEventListener("click", () => {
    setLanguage(button.getAttribute("data-lang-option") || "en");
  });
});

document.querySelectorAll("[data-scroll-target]").forEach((button) => {
  button.addEventListener("click", () => {
    const targetId = button.getAttribute("data-scroll-target");
    const direction = Number(button.getAttribute("data-scroll-dir") || "1");
    const target = targetId ? document.getElementById(targetId) : null;
    if (!target) {
      return;
    }

    const amount = Math.max(260, Math.round(target.clientWidth * 0.82));
    target.scrollBy({ left: amount * direction, behavior: "smooth" });
  });
});

document.querySelectorAll(".gallery-item").forEach((item) => {
  item.addEventListener("click", () => {
    const full = item.getAttribute("data-full");
    const title = currentLanguage === "pl"
      ? item.getAttribute("data-title-pl") || item.getAttribute("data-title")
      : item.getAttribute("data-title");

    if (!full || !dialog || !dialogImage || !dialogTitle) {
      return;
    }

    dialogImage.src = full;
    dialogImage.alt = title || "STARJACKERS";
    dialogTitle.textContent = title || "STARJACKERS";

    if (typeof dialog.showModal === "function") {
      dialog.showModal();
    } else {
      window.open(full, "_blank", "noopener");
    }
  });
});

if (dialog && dialogClose) {
  dialogClose.addEventListener("click", () => dialog.close());
  dialog.addEventListener("click", (event) => {
    if (event.target === dialog) {
      dialog.close();
    }
  });
}

const revealItems = document.querySelectorAll(".reveal");

if ("IntersectionObserver" in window) {
  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        entry.target.classList.add("is-visible");
        observer.unobserve(entry.target);
      }
    });
  }, { threshold: 0.15 });

  revealItems.forEach((item) => observer.observe(item));
} else {
  revealItems.forEach((item) => item.classList.add("is-visible"));
}
